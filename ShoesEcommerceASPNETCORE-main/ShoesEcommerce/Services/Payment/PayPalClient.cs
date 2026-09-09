using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShoesEcommerce.Models.Payments.PayPal;

namespace ShoesEcommerce.Services.Payment
{
    public sealed class PayPalClient
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _mode;
        private readonly ILogger<PayPalClient> _logger;
        private readonly HttpClient _httpClient;

        public string BaseUrl => _mode == "Live"
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        public PayPalClient(
            string clientId, 
            string clientSecret, 
            string mode,
            ILogger<PayPalClient> logger,
            IHttpClientFactory httpClientFactory)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            _mode = mode ?? "Sandbox";
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClientFactory.CreateClient("PayPal");
            
            _logger.LogInformation("PayPalClient initialized in {Mode} mode", _mode);
        }

        /// <summary>
        /// Authenticate with PayPal to get access token
        /// </summary>
        private async Task<AuthResponse> AuthenticateAsync()
        {
            try
            {
                _logger.LogInformation("Authenticating with PayPal...");

                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
                var content = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "client_credentials")
                };

                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri($"{BaseUrl}/v1/oauth2/token"),
                    Method = HttpMethod.Post,
                    Headers =
                    {
                        { "Authorization", $"Basic {auth}" }
                    },
                    Content = new FormUrlEncodedContent(content)
                };

                var httpResponse = await _httpClient.SendAsync(request);
                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("PayPal authentication failed: {StatusCode} - {Response}", 
                        httpResponse.StatusCode, jsonResponse);
                    throw new InvalidOperationException($"PayPal authentication failed: {httpResponse.StatusCode}");
                }

                var response = JsonSerializer.Deserialize<AuthResponse>(jsonResponse);
                
                if (response == null)
                {
                    _logger.LogError("Failed to deserialize PayPal auth response");
                    throw new InvalidOperationException("Failed to deserialize PayPal auth response");
                }

                _logger.LogInformation("PayPal authentication successful");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PayPal authentication");
                throw;
            }
        }

        /// <summary>
        /// Create a PayPal order with item details, invoice ID, shipping address, and amount breakdown
        /// </summary>
        public async Task<CreateOrderResponse> CreateOrderAsync(
            decimal subtotal,
            decimal discountAmount,
            decimal totalAmount,
            string referenceId,
            string returnUrl,
            string cancelUrl,
            string? description = null,
            string? invoiceId = null,
            List<Item>? items = null,
            Shipping? shippingAddress = null)
        {
            try
            {
                _logger.LogInformation(
                    "Creating PayPal order - Subtotal: {Subtotal} VND, Discount: {Discount} VND, Total: {Total} VND, Reference: {Reference}, InvoiceId: {InvoiceId}, HasShipping: {HasShipping}",
                    subtotal, discountAmount, totalAmount, referenceId, invoiceId, shippingAddress != null);

                var auth = await AuthenticateAsync();

                // Convert VND to USD (approximate rate: 1 USD = 24,000 VND)
                const decimal vndToUsdRate = 24000m;
                var subtotalInUsd = Math.Round(subtotal / vndToUsdRate, 2);
                var discountInUsd = discountAmount > 0 ? Math.Round(discountAmount / vndToUsdRate, 2) : 0m;
                var totalInUsd = Math.Round(totalAmount / vndToUsdRate, 2);

                _logger.LogInformation(
                    "Converted to USD - Subtotal: ${SubtotalUsd}, Discount: ${DiscountUsd}, Total: ${TotalUsd}",
                    subtotalInUsd, discountInUsd, totalInUsd);

                // Convert items to USD
                List<Item>? itemsInUsd = null;
                decimal itemTotalUsd = 0m;
                
                if (items != null && items.Any())
                {
                    itemsInUsd = items.Select(item => new Item
                    {
                        name = TruncateString(item.name, 127), // PayPal limit
                        quantity = item.quantity,
                        description = TruncateString(item.description, 127),
                        sku = item.sku,
                        category = item.category ?? "PHYSICAL_GOODS",
                        unit_amount = new UnitAmount
                        {
                            currency_code = "USD",
                            value = (Math.Round(decimal.Parse(item.unit_amount.value) / vndToUsdRate, 2))
                                .ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                        }
                    }).ToList();

                    // Calculate item_total from converted items to avoid rounding mismatch
                    itemTotalUsd = itemsInUsd.Sum(i => 
                        decimal.Parse(i.unit_amount.value, System.Globalization.CultureInfo.InvariantCulture) * 
                        int.Parse(i.quantity));
                    itemTotalUsd = Math.Round(itemTotalUsd, 2);

                    _logger.LogInformation("PayPal order includes {ItemCount} items, ItemTotal: ${ItemTotal}", 
                        itemsInUsd.Count, itemTotalUsd);
                }
                else
                {
                    // No items provided - use subtotal as item_total
                    itemTotalUsd = subtotalInUsd;
                }

                // Calculate final amount from item_total and discount to ensure they match
                decimal calculatedTotalUsd;
                if (discountInUsd > 0)
                {
                    calculatedTotalUsd = itemTotalUsd - discountInUsd;
                }
                else
                {
                    calculatedTotalUsd = itemTotalUsd;
                }
                calculatedTotalUsd = Math.Round(calculatedTotalUsd, 2);

                // Log if there's a difference between calculated and expected total
                if (Math.Abs(calculatedTotalUsd - totalInUsd) > 0.01m)
                {
                    _logger.LogWarning(
                        "PayPal amount adjustment needed - Original Total: ${Original}, Calculated: ${Calculated} (item_total: ${ItemTotal} - discount: ${Discount}). Using calculated value.",
                        totalInUsd, calculatedTotalUsd, itemTotalUsd, discountInUsd);
                }

                // Build amount object - use calculated total to ensure breakdown matches
                var amount = new Amount
                {
                    currency_code = "USD",
                    value = calculatedTotalUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                };

                // Only add breakdown if we have items OR discount
                if (itemsInUsd != null && itemsInUsd.Any())
                {
                    if (discountInUsd > 0)
                    {
                        amount.breakdown = new Breakdown
                        {
                            item_total = new Amount
                            {
                                currency_code = "USD",
                                value = itemTotalUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                            },
                            discount = new Amount
                            {
                                currency_code = "USD",
                                value = discountInUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                            }
                        };
                    }
                    else
                    {
                        // No discount - item_total should equal amount.value
                        amount.breakdown = new Breakdown
                        {
                            item_total = new Amount
                            {
                                currency_code = "USD",
                                value = itemTotalUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                            }
                        };
                        
                        // Ensure amount.value equals item_total when no discount
                        amount.value = itemTotalUsd.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                _logger.LogInformation(
                    "Final PayPal amounts - Value: ${Value}, ItemTotal: ${ItemTotal}, Discount: ${Discount}",
                    amount.value, 
                    amount.breakdown?.item_total?.value ?? "N/A",
                    amount.breakdown?.discount?.value ?? "0");

                var purchaseUnit = new PurchaseUnit
                {
                    reference_id = referenceId,
                    description = TruncateString(description ?? "ShoesEcommerce Order", 127),
                    invoice_id = invoiceId,
                    custom_id = referenceId,
                    amount = amount,
                    items = itemsInUsd,
                    shipping = shippingAddress // ? NEW: Add shipping address
                };

                // ? Log shipping if provided
                if (shippingAddress != null)
                {
                    _logger.LogInformation("PayPal order includes shipping: Name={Name}, Address={Address}, City={City}",
                        shippingAddress.name?.full_name,
                        shippingAddress.address?.address_line_1,
                        shippingAddress.address?.admin_area_2);
                }

                var request = new CreateOrderRequest
                {
                    intent = "CAPTURE",
                    application_context = new ApplicationContext
                    {
                        brand_name = "SPORTS Vietnam",
                        landing_page = "BILLING",
                        user_action = "PAY_NOW",
                        return_url = returnUrl,
                        cancel_url = cancelUrl
                    },
                    purchase_units = new List<PurchaseUnit> { purchaseUnit }
                };

                _httpClient.DefaultRequestHeaders.Authorization = 
                    AuthenticationHeaderValue.Parse($"Bearer {auth.access_token}");

                var httpResponse = await _httpClient.PostAsJsonAsync(
                    $"{BaseUrl}/v2/checkout/orders", 
                    request);

                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create PayPal order: {StatusCode} - {Response}",
                        httpResponse.StatusCode, jsonResponse);
                    throw new InvalidOperationException($"Failed to create PayPal order: {httpResponse.StatusCode} - {jsonResponse}");
                }

                var response = JsonSerializer.Deserialize<CreateOrderResponse>(jsonResponse);

                if (response == null)
                {
                    _logger.LogError("Failed to deserialize PayPal create order response");
                    throw new InvalidOperationException("Failed to deserialize PayPal create order response");
                }

                _logger.LogInformation("PayPal order created successfully: OrderId={OrderId}, Status={Status}",
                    response.id, response.status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal order");
                throw;
            }
        }

        /// <summary>
        /// Truncate string to max length for PayPal API limits
        /// </summary>
        private static string? TruncateString(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }

        /// <summary>
        /// Capture payment for a PayPal order
        /// </summary>
        public async Task<CaptureOrderResponse> CaptureOrderAsync(string orderId)
        {
            try
            {
                _logger.LogInformation("Capturing PayPal order: {OrderId}", orderId);

                var auth = await AuthenticateAsync();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    AuthenticationHeaderValue.Parse($"Bearer {auth.access_token}");

                var httpContent = new StringContent("", Encoding.Default, "application/json");

                var httpResponse = await _httpClient.PostAsync(
                    $"{BaseUrl}/v2/checkout/orders/{orderId}/capture", 
                    httpContent);

                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to capture PayPal order {OrderId}: {StatusCode} - {Response}",
                        orderId, httpResponse.StatusCode, jsonResponse);
                    throw new InvalidOperationException($"Failed to capture PayPal order: {httpResponse.StatusCode}");
                }

                var response = JsonSerializer.Deserialize<CaptureOrderResponse>(jsonResponse);

                if (response == null)
                {
                    _logger.LogError("Failed to deserialize PayPal capture response for order {OrderId}", orderId);
                    throw new InvalidOperationException("Failed to deserialize PayPal capture response");
                }

                _logger.LogInformation("PayPal order captured successfully: OrderId={OrderId}, Status={Status}",
                    response.id, response.status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing PayPal order {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Get order details from PayPal
        /// </summary>
        public async Task<CaptureOrderResponse> GetOrderDetailsAsync(string orderId)
        {
            try
            {
                _logger.LogInformation("Getting PayPal order details: {OrderId}", orderId);

                var auth = await AuthenticateAsync();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    AuthenticationHeaderValue.Parse($"Bearer {auth.access_token}");

                var httpResponse = await _httpClient.GetAsync(
                    $"{BaseUrl}/v2/checkout/orders/{orderId}");

                var jsonResponse = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get PayPal order details {OrderId}: {StatusCode} - {Response}",
                        orderId, httpResponse.StatusCode, jsonResponse);
                    throw new InvalidOperationException($"Failed to get PayPal order details: {httpResponse.StatusCode}");
                }

                var response = JsonSerializer.Deserialize<CaptureOrderResponse>(jsonResponse);

                if (response == null)
                {
                    _logger.LogError("Failed to deserialize PayPal order details for {OrderId}", orderId);
                    throw new InvalidOperationException("Failed to deserialize PayPal order details");
                }

                _logger.LogInformation("PayPal order details retrieved: OrderId={OrderId}, Status={Status}",
                    response.id, response.status);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PayPal order details {OrderId}", orderId);
                throw;
            }
        }
    }
}
