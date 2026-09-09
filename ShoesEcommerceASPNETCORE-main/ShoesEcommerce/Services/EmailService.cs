using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ShoesEcommerce.Models.Orders;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services.Options;

namespace ShoesEcommerce.Services
{
    /// <summary>
    /// Email service implementation supporting SMTP and Mailchimp Marketing API
    /// Mailchimp Merge Tags Reference:
    /// - EMAIL (*|EMAIL|*), FNAME (*|FNAME|*), LNAME (*|LNAME|*)
    /// - ADDRESS (*|ADDRESS|*), PHONE (*|PHONE|*), BIRTHDAY (*|BIRTHDAY|*)
    /// - COMPANY (*|COMPANY|*), ORDERNO (*|ORDERNO|*), INVNO (*|INVNO|*)
    /// - ODATE (*|ODATE|*), OTOTAL (*|OTOTAL|*), PAYMENT (*|PAYMENT|*)
    /// - SNAME (*|SNAME|*), SPHONE (*|SPHONE|*), SADDR (*|SADDR|*)
    /// - TRACKNO (*|TRACKNO|*), ORDERURL (*|ORDERURL|*), SHOPURL (*|SHOPURL|*)
    /// - RESETURL (*|RESETURL|*), CARTURL (*|CARTURL|*), CANCELRSN (*|CANCELRSN|*)
    /// - PROMO (*|PROMO|*), DISCOUNT (*|DISCOUNT|*), EXDATE (*|EXDATE|*)
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;

        // Mailchimp merge tag constants
        private static class MergeTags
        {
            public const string Email = "EMAIL";
            public const string FirstName = "FNAME";
            public const string LastName = "LNAME";
            public const string Address = "ADDRESS";
            public const string Phone = "PHONE";
            public const string Birthday = "BIRTHDAY";
            public const string Company = "COMPANY";
            public const string OrderNumber = "ORDERNO";
            public const string InvoiceNumber = "INVNO";
            public const string OrderDate = "ODATE";
            public const string OrderTotal = "OTOTAL";
            public const string PaymentMethod = "PAYMENT";
            public const string ShippingName = "SNAME";
            public const string ShippingPhone = "SPHONE";
            public const string ShippingAddress = "SADDR";
            public const string TrackingNumber = "TRACKNO";
            public const string OrderUrl = "ORDERURL";
            public const string ShopUrl = "SHOPURL";
            public const string ResetPasswordUrl = "RESETURL";
            public const string CartUrl = "CARTURL";
            public const string CancelReason = "CANCELRSN";
            public const string PromoCode = "PROMO";
            public const string DiscountPercent = "DISCOUNT";
            public const string ExpiryDate = "EXDATE";
        }

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }

        #region Mailchimp API Methods

        private string GetMailchimpBaseUrl()
        {
            var prefix = _settings.MailchimpServerPrefix;
            if (string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(_settings.MailchimpApiKey))
            {
                var parts = _settings.MailchimpApiKey.Split('-');
                if (parts.Length > 1)
                    prefix = parts[^1];
            }
            return $"https://{prefix}.api.mailchimp.com/3.0";
        }

        private HttpClient CreateMailchimpClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(GetMailchimpBaseUrl());
            
            var authBytes = Encoding.ASCII.GetBytes($"anystring:{_settings.MailchimpApiKey}");
            client.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            return client;
        }

        /// <summary>
        /// Add or update subscriber in Mailchimp audience with merge fields
        /// </summary>
        public async Task<bool> AddOrUpdateSubscriberAsync(string email, string firstName, string lastName, Dictionary<string, string>? mergeFields = null)
        {
            try
            {
                using var client = CreateMailchimpClient();
                var emailHash = CreateMD5Hash(email.ToLower());
                
                var allMergeFields = new Dictionary<string, string>
                {
                    [MergeTags.FirstName] = firstName ?? "",
                    [MergeTags.LastName] = lastName ?? "",
                    [MergeTags.ShopUrl] = _settings.WebsiteUrl ?? ""
                };
                
                if (mergeFields != null)
                {
                    foreach (var field in mergeFields)
                    {
                        allMergeFields[field.Key] = field.Value;
                    }
                }

                var subscriber = new
                {
                    email_address = email,
                    status_if_new = "subscribed",
                    merge_fields = allMergeFields
                };

                var json = JsonSerializer.Serialize(subscriber);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync(
                    $"/lists/{_settings.MailchimpAudienceId}/members/{emailHash}", 
                    content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Subscriber {Email} added/updated in Mailchimp", email);
                    return true;
                }

                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Mailchimp subscriber error: {Status} - {Error}", response.StatusCode, errorBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding subscriber {Email} to Mailchimp", email);
                return false;
            }
        }

        /// <summary>
        /// Update subscriber merge fields for order-related emails
        /// </summary>
        private async Task<bool> UpdateSubscriberForOrderAsync(Order order)
        {
            var customerEmail = order.Customer?.Email;
            if (string.IsNullOrEmpty(customerEmail)) return false;

            var shippingAddress = order.ShippingAddress;
            var invoiceNumber = order.Invoice?.InvoiceNumber ?? $"INV-{order.Id}-{DateTime.UtcNow:yyyyMMdd}";
            var paymentMethod = order.Payment?.Method ?? "COD";
            
            var mergeFields = new Dictionary<string, string>
            {
                [MergeTags.OrderNumber] = $"ORD{order.Id:D6}",
                [MergeTags.InvoiceNumber] = invoiceNumber,
                [MergeTags.OrderDate] = order.CreatedAt.ToString("dd/MM/yyyy"),
                [MergeTags.OrderTotal] = order.TotalAmount.ToString("N0") + "?",
                [MergeTags.PaymentMethod] = paymentMethod,
                [MergeTags.ShippingName] = shippingAddress?.FullName ?? "",
                [MergeTags.ShippingPhone] = shippingAddress?.PhoneNumber ?? "",
                [MergeTags.ShippingAddress] = FormatShippingAddress(shippingAddress),
                [MergeTags.OrderUrl] = $"{_settings.WebsiteUrl}/don-hang/{order.Id}",
                [MergeTags.ShopUrl] = _settings.WebsiteUrl ?? "",
                [MergeTags.CartUrl] = $"{_settings.WebsiteUrl}/gio-hang"
            };

            return await AddOrUpdateSubscriberAsync(
                customerEmail,
                order.Customer?.FirstName ?? "",
                order.Customer?.LastName ?? "",
                mergeFields);
        }

        private string FormatShippingAddress(ShippingAddress? address)
        {
            if (address == null) return "";
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(address.Address)) parts.Add(address.Address);
            if (!string.IsNullOrEmpty(address.District)) parts.Add(address.District);
            if (!string.IsNullOrEmpty(address.City)) parts.Add(address.City);
            return string.Join(", ", parts);
        }

        private static string CreateMD5Hash(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        #endregion

        #region SMTP Methods

        private async Task<bool> SendViaSmtpAsync(string to, string subject, string htmlBody)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent via SMTP to {Email}: {Subject}", to, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SMTP to {Email}", to);
                return false;
            }
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };
        }

        #endregion

        #region IEmailService Implementation

        public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("Email sending disabled. Would send to {Email}: {Subject}", to, subject);
                return true;
            }

            try
            {
                return await SendViaSmtpAsync(to, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}: {Subject}", to, subject);
                return false;
            }
        }

        public async Task<bool> SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody)
        {
            var tasks = recipients.Select(r => SendEmailAsync(r, subject, htmlBody));
            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }

        /// <summary>
        /// Send promotion campaign using Mailchimp
        /// Template uses merge tags: *|PROMO|*, *|DISCOUNT|*, *|EXDATE|*, *|SHOPURL|*, *|FNAME|*
        /// </summary>
        public async Task<bool> SendPromotionCampaignAsync(
            string campaignTitle,
            string subject,
            string promoCode,
            int discountPercent,
            DateTime expiryDate,
            List<PromotionProduct>? featuredProducts = null)
        {
            try
            {
                // Update all subscribers with promo merge fields
                await UpdateAllSubscribersPromoFieldsAsync(promoCode, discountPercent, expiryDate);
                
                var htmlContent = BuildPromotionCampaignHtml(campaignTitle, promoCode, discountPercent, expiryDate, featuredProducts);
                var previewText = $"?? Gi?m {discountPercent}% v?i mã {promoCode} - Ch? ??n {expiryDate:dd/MM/yyyy}!";

                return await CreateAndSendCampaignAsync(campaignTitle, subject, previewText, htmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending promotion campaign: {Title}", campaignTitle);
                return false;
            }
        }

        private async Task UpdateAllSubscribersPromoFieldsAsync(string promoCode, int discountPercent, DateTime expiryDate)
        {
            try
            {
                using var client = CreateMailchimpClient();
                
                var response = await client.GetAsync($"/lists/{_settings.MailchimpAudienceId}/members?count=1000&status=subscribed");
                if (!response.IsSuccessStatusCode) return;

                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                var members = doc.RootElement.GetProperty("members");

                foreach (var member in members.EnumerateArray())
                {
                    var email = member.GetProperty("email_address").GetString();
                    if (string.IsNullOrEmpty(email)) continue;

                    var mergeFields = new Dictionary<string, string>
                    {
                        [MergeTags.PromoCode] = promoCode,
                        [MergeTags.DiscountPercent] = discountPercent.ToString(),
                        [MergeTags.ExpiryDate] = expiryDate.ToString("dd/MM/yyyy"),
                        [MergeTags.ShopUrl] = _settings.WebsiteUrl ?? ""
                    };

                    var emailHash = CreateMD5Hash(email.ToLower());
                    var updateData = new { merge_fields = mergeFields };
                    var json = JsonSerializer.Serialize(updateData);
                    var updateContent = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    await client.PatchAsync($"/lists/{_settings.MailchimpAudienceId}/members/{emailHash}", updateContent);
                }

                _logger.LogInformation("Updated promo fields for all subscribers: {Code}, {Discount}%", promoCode, discountPercent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating promo fields for subscribers");
            }
        }

        /// <summary>
        /// Create and send a campaign via Mailchimp
        /// </summary>
        public async Task<bool> CreateAndSendCampaignAsync(
            string campaignName,
            string subject,
            string previewText,
            string htmlContent,
            List<string>? recipientEmails = null)
        {
            try
            {
                using var client = CreateMailchimpClient();

                var campaignData = new
                {
                    type = "regular",
                    recipients = new
                    {
                        list_id = _settings.MailchimpAudienceId
                    },
                    settings = new
                    {
                        subject_line = subject,
                        preview_text = previewText,
                        title = campaignName,
                        from_name = _settings.SenderName,
                        reply_to = _settings.SenderEmail
                    }
                };

                var createJson = JsonSerializer.Serialize(campaignData);
                var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
                
                var createResponse = await client.PostAsync("/campaigns", createContent);
                if (!createResponse.IsSuccessStatusCode)
                {
                    var error = await createResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create campaign: {Error}", error);
                    return false;
                }

                var createResult = await createResponse.Content.ReadAsStringAsync();
                var campaignId = JsonDocument.Parse(createResult).RootElement.GetProperty("id").GetString();

                _logger.LogInformation("Campaign created: {CampaignId}", campaignId);

                var contentData = new { html = htmlContent };
                var contentJson = JsonSerializer.Serialize(contentData);
                var contentBody = new StringContent(contentJson, Encoding.UTF8, "application/json");
                
                var contentResponse = await client.PutAsync($"/campaigns/{campaignId}/content", contentBody);
                if (!contentResponse.IsSuccessStatusCode)
                {
                    var error = await contentResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to set campaign content: {Error}", error);
                    return false;
                }

                var sendResponse = await client.PostAsync($"/campaigns/{campaignId}/actions/send", null);
                if (!sendResponse.IsSuccessStatusCode)
                {
                    var error = await sendResponse.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send campaign: {Error}", error);
                    return false;
                }

                _logger.LogInformation("Campaign {CampaignId} sent successfully!", campaignId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/sending campaign");
                return false;
            }
        }

        private string BuildPromotionCampaignHtml(
            string title,
            string promoCode,
            int discountPercent,
            DateTime expiryDate,
            List<PromotionProduct>? products)
        {
            var productsHtml = "";
            if (products != null && products.Any())
            {
                productsHtml = @"<h3 style=""color:#1a1a1a;font-size:18px;margin:30px 0 16px;"">?? S?n ph?m n?i b?t</h3>
                <table width=""100%"" cellpadding=""0"" cellspacing=""0"">";
                
                foreach (var product in products.Take(4))
                {
                    productsHtml += $@"
                    <tr>
                        <td style=""padding:12px;border-bottom:1px solid #e5e7eb;"">
                            <table width=""100%"">
                                <tr>
                                    <td width=""80"" style=""vertical-align:top;"">
                                        <img src=""{product.ImageUrl}"" alt=""{product.Name}"" style=""width:80px;height:80px;object-fit:cover;border-radius:8px;"">
                                    </td>
                                    <td style=""padding-left:12px;vertical-align:top;"">
                                        <p style=""margin:0;font-weight:600;color:#1a1a1a;"">{product.Name}</p>
                                        <p style=""margin:4px 0 0;color:#ef4444;font-size:14px;text-decoration:line-through;"">{product.OriginalPrice:N0}?</p>
                                        <p style=""margin:4px 0 0;color:#22c55e;font-size:18px;font-weight:700;"">{product.SalePrice:N0}?</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>";
                }
                productsHtml += "</table>";
            }

            // Use Mailchimp merge tags: *|PROMO|*, *|DISCOUNT|*, *|EXDATE|*, *|SHOPURL|*, *|FNAME|*
            var content = $@"
<div style=""text-align:center;margin-bottom:30px;"">
    <h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:28px;"">?? {title}</h2>
    <p style=""color:#6b7280;margin:0;font-size:16px;"">Xin chào *|FNAME|*, ?u ?ãi ??c bi?t dành riêng cho b?n!</p>
</div>

<div style=""background:linear-gradient(135deg,#dc2626 0%,#991b1b 100%);border-radius:16px;padding:30px;text-align:center;margin-bottom:30px;"">
    <p style=""color:#fecaca;margin:0 0 8px;font-size:14px;text-transform:uppercase;letter-spacing:2px;"">Mã gi?m giá</p>
    <p style=""color:white;margin:0;font-size:36px;font-weight:700;letter-spacing:4px;"">{promoCode}</p>
    <p style=""color:#fecaca;margin:12px 0 0;font-size:16px;"">Gi?m ngay <strong style=""color:white;font-size:24px;"">{discountPercent}%</strong></p>
</div>

<div style=""background:#fef3c7;border-left:4px solid #f59e0b;padding:16px;border-radius:8px;margin-bottom:20px;"">
    <p style=""margin:0;color:#92400e;font-size:14px;"">
        ? <strong>Th?i h?n:</strong> ??n h?t ngày {expiryDate:dd/MM/yyyy}
    </p>
</div>

{productsHtml}

<div style=""text-align:center;margin-top:30px;"">
    <a href=""{_settings.WebsiteUrl}/san-pham"" 
       style=""display:inline-block;background:#1a1a1a;color:white;padding:16px 40px;border-radius:8px;text-decoration:none;font-weight:600;font-size:16px;"">
        ?? Mua s?m ngay
    </a>
</div>

<p style=""color:#9ca3af;font-size:12px;text-align:center;margin-top:20px;"">
    Áp d?ng cho t?t c? s?n ph?m. Không áp d?ng cùng khuy?n mãi khác.
</p>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        public async Task<bool> SendOrderConfirmationAsync(Order order)
        {
            try
            {
                var customerEmail = order.Customer?.Email;
                if (string.IsNullOrEmpty(customerEmail)) return false;

                await UpdateSubscriberForOrderAsync(order);

                var customerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim();
                var subject = $"? Xác nh?n ??n hàng #ORD{order.Id:D6} - SPORTS Vietnam";
                var htmlBody = BuildOrderConfirmationEmail(order, customerName);

                return await SendEmailAsync(customerEmail, subject, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation for order {OrderId}", order.Id);
                return false;
            }
        }

        public async Task<bool> SendOrderProcessingAsync(Order order)
        {
            var customerEmail = order.Customer?.Email;
            if (string.IsNullOrEmpty(customerEmail)) return false;

            await UpdateSubscriberForOrderAsync(order);

            var customerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim();
            var subject = $"?? ??n hàng #ORD{order.Id:D6} ?ang ???c x? lý - SPORTS Vietnam";
            var htmlBody = BuildOrderProcessingEmail(order, customerName);

            return await SendEmailAsync(customerEmail, subject, htmlBody);
        }

        public async Task<bool> SendOrderShippedAsync(Order order, string? trackingNumber = null)
        {
            var customerEmail = order.Customer?.Email;
            if (string.IsNullOrEmpty(customerEmail)) return false;

            var mergeFields = new Dictionary<string, string>
            {
                [MergeTags.TrackingNumber] = trackingNumber ?? "",
                [MergeTags.OrderNumber] = $"ORD{order.Id:D6}",
                [MergeTags.OrderUrl] = $"{_settings.WebsiteUrl}/don-hang/{order.Id}"
            };
            await AddOrUpdateSubscriberAsync(customerEmail, order.Customer?.FirstName ?? "", order.Customer?.LastName ?? "", mergeFields);

            var customerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim();
            var subject = $"?? ??n hàng #ORD{order.Id:D6} ?ang ???c giao - SPORTS Vietnam";
            var htmlBody = BuildOrderShippedEmail(order, customerName, trackingNumber);

            return await SendEmailAsync(customerEmail, subject, htmlBody);
        }

        public async Task<bool> SendOrderDeliveredAsync(Order order)
        {
            var customerEmail = order.Customer?.Email;
            if (string.IsNullOrEmpty(customerEmail)) return false;

            var customerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim();
            var subject = $"? ??n hàng #ORD{order.Id:D6} ?ã giao thành công - SPORTS Vietnam";
            var htmlBody = BuildOrderDeliveredEmail(order, customerName);

            return await SendEmailAsync(customerEmail, subject, htmlBody);
        }

        public async Task<bool> SendOrderCancelledAsync(Order order, string? reason = null)
        {
            var customerEmail = order.Customer?.Email;
            if (string.IsNullOrEmpty(customerEmail)) return false;

            var mergeFields = new Dictionary<string, string>
            {
                [MergeTags.CancelReason] = reason ?? "Không có lý do c? th?",
                [MergeTags.OrderNumber] = $"ORD{order.Id:D6}",
                [MergeTags.ShopUrl] = _settings.WebsiteUrl ?? ""
            };
            await AddOrUpdateSubscriberAsync(customerEmail, order.Customer?.FirstName ?? "", order.Customer?.LastName ?? "", mergeFields);

            var customerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim();
            var subject = $"? ??n hàng #ORD{order.Id:D6} ?ã b? h?y - SPORTS Vietnam";
            var htmlBody = BuildOrderCancelledEmail(order, customerName, reason);

            return await SendEmailAsync(customerEmail, subject, htmlBody);
        }

        public async Task<bool> SendAbandonedCartReminderAsync(string email, string customerName, List<CartItemEmailModel> items)
        {
            var mergeFields = new Dictionary<string, string>
            {
                [MergeTags.CartUrl] = $"{_settings.WebsiteUrl}/gio-hang",
                [MergeTags.ShopUrl] = _settings.WebsiteUrl ?? ""
            };
            await AddOrUpdateSubscriberAsync(email, customerName, "", mergeFields);

            var subject = "?? B?n còn s?n ph?m trong gi? hàng - SPORTS Vietnam";
            var htmlBody = BuildAbandonedCartEmail(customerName, items);
            return await SendEmailAsync(email, subject, htmlBody);
        }

        public async Task<bool> SendAccountActivationAsync(string email, string customerName, string activationLink)
        {
            var subject = "?? Kích ho?t tài kho?n SPORTS Vietnam";
            var htmlBody = BuildAccountActivationEmail(customerName, activationLink);
            return await SendEmailAsync(email, subject, htmlBody);
        }

        public async Task<bool> SendPasswordResetAsync(string email, string customerName, string resetLink)
        {
            var mergeFields = new Dictionary<string, string>
            {
                [MergeTags.ResetPasswordUrl] = resetLink,
                [MergeTags.ShopUrl] = _settings.WebsiteUrl ?? ""
            };
            await AddOrUpdateSubscriberAsync(email, customerName, "", mergeFields);

            var subject = "?? ??t l?i m?t kh?u - SPORTS Vietnam";
            var htmlBody = BuildPasswordResetEmail(customerName, resetLink);
            return await SendEmailAsync(email, subject, htmlBody);
        }

        public async Task<bool> SendWelcomeEmailAsync(string email, string customerName)
        {
            var mergeFields = new Dictionary<string, string>
            {
                [MergeTags.PromoCode] = "WELCOME10",
                [MergeTags.DiscountPercent] = "10",
                [MergeTags.ShopUrl] = _settings.WebsiteUrl ?? ""
            };
            await AddOrUpdateSubscriberAsync(email, customerName, "", mergeFields);
            
            var subject = "?? Chào m?ng ??n v?i SPORTS Vietnam!";
            var htmlBody = BuildWelcomeEmail(customerName);
            
            return await SendEmailAsync(email, subject, htmlBody);
        }

        #endregion

        #region Email Templates

        private string GetBaseTemplate() => $@"
<!DOCTYPE html>
<html lang=""vi"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin:0;padding:0;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background-color:#f5f5f5;"">
    <center>
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#f5f5f5;"">
            <tr>
                <td align=""center"" style=""padding:40px 20px;"">
                    <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff;border-radius:16px;overflow:hidden;"">
                        <tr>
                            <td style=""background:linear-gradient(135deg,#1a1a1a 0%,#333333 100%);padding:30px;text-align:center;"">
                                <h1 style=""color:#ffffff;margin:0;font-size:28px;font-weight:700;letter-spacing:2px;"">SPORTS Vietnam</h1>
                                <p style=""color:#cccccc;margin:10px 0 0;font-size:14px;"">Authentic Sports Gear</p>
                            </td>
                        </tr>
                        <tr>
                            <td style=""padding:40px 30px;"">
                                {{{{CONTENT}}}}
                            </td>
                        </tr>
                        <tr>
                            <td style=""background-color:#f8f9fa;padding:30px;text-align:center;border-top:1px solid #e5e7eb;"">
                                <p style=""color:#6b7280;font-size:14px;margin:0 0 10px;""><strong>SPORTS Vietnam</strong></p>
                                <p style=""color:#9ca3af;font-size:12px;margin:0;"">?? 316 Ngô Gia T?, Ph??ng V?n Lâi, H? Chí Minh</p>
                                <p style=""color:#9ca3af;font-size:12px;margin:5px 0;"">?? 0901 123 456 | ?? support@sportsvietnam.com</p>
                                <p style=""color:#9ca3af;font-size:11px;margin:15px 0 0;"">© {DateTime.Now.Year} SPORTS Vietnam</p>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </center>
</body>
</html>";

        private string BuildOrderConfirmationEmail(Order order, string customerName)
        {
            var itemsHtml = BuildOrderItemsHtml(order);
            var invoiceNumber = order.Invoice?.InvoiceNumber ?? $"INV-{order.Id}-{DateTime.UtcNow:yyyyMMdd}";
            var shippingAddress = FormatShippingAddress(order.ShippingAddress);

            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">C?m ?n b?n ?ã ??t hàng! ?</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">Xin chào <strong>{customerName}</strong>, ??n hàng ?ã ???c xác nh?n.</p>

<div style=""background:#f8f9fa;border-radius:12px;padding:24px;margin-bottom:30px;"">
    <table width=""100%""><tr>
        <td><p style=""color:#6b7280;font-size:12px;margin:0;"">MÃ ??N HÀNG</p><p style=""color:#1a1a1a;font-size:20px;font-weight:700;margin:4px 0 0;"">#ORD{order.Id:D6}</p></td>
        <td align=""right""><p style=""color:#6b7280;font-size:12px;margin:0;"">S? HÓA ??N</p><p style=""color:#0066b3;font-size:14px;font-weight:600;margin:4px 0 0;"">{invoiceNumber}</p></td>
    </tr></table>
</div>

<div style=""background:#e0f2fe;border-radius:12px;padding:16px;margin-bottom:20px;"">
    <p style=""margin:0;color:#0369a1;font-size:13px;""><strong>?? ??a ch? giao hàng:</strong></p>
    <p style=""margin:8px 0 0;color:#1e40af;"">{order.ShippingAddress?.FullName} - {order.ShippingAddress?.PhoneNumber}</p>
    <p style=""margin:4px 0 0;color:#6b7280;font-size:13px;"">{shippingAddress}</p>
</div>

<h3 style=""color:#1a1a1a;font-size:16px;margin:0 0 16px;"">?? S?n ph?m</h3>
{itemsHtml}

<div style=""background:#1a1a1a;border-radius:12px;padding:20px;margin:24px 0;"">
    <table width=""100%"">
        <tr>
            <td style=""color:white;font-size:18px;font-weight:700;"">T?ng c?ng</td>
            <td style=""color:white;font-size:24px;font-weight:700;text-align:right;"">{order.TotalAmount:N0}?</td>
        </tr>
    </table>
</div>

<div style=""text-align:center;"">
    <a href=""{_settings.WebsiteUrl}/don-hang/{order.Id}"" style=""display:inline-block;background:#1a1a1a;color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">Xem chi ti?t ??n hàng</a>
</div>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildOrderProcessingEmail(Order order, string customerName)
        {
            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">??n hàng ?ang ???c x? lý ??</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">Xin chào <strong>{customerName}</strong>, ??n hàng <strong>#ORD{order.Id:D6}</strong> ?ang ???c chu?n b?.</p>

<div style=""background:#fef3c7;border-left:4px solid #f59e0b;padding:20px;border-radius:8px;"">
    <p style=""margin:0;color:#92400e;""><strong>? ?ang x? lý</strong><br>??n hàng ?ang ???c ?óng gói. B?n s? nh?n ???c thông báo khi ??n hàng ???c giao cho ??n v? v?n chuy?n.</p>
</div>

<div style=""text-align:center;margin-top:30px;"">
    <a href=""{_settings.WebsiteUrl}/don-hang/{order.Id}"" style=""display:inline-block;background:#1a1a1a;color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">Theo dõi ??n hàng</a>
</div>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildOrderShippedEmail(Order order, string customerName, string? trackingNumber)
        {
            var trackingHtml = !string.IsNullOrEmpty(trackingNumber) 
                ? $"<p style=\"margin:8px 0 0;\">?? Mã v?n ??n: <strong>{trackingNumber}</strong></p>" : "";

            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">??n hàng ?ang giao! ??</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">??n hàng <strong>#ORD{order.Id:D6}</strong> ?ã ???c giao cho ??n v? v?n chuy?n.</p>

<div style=""background:#e0f2fe;border-left:4px solid #0ea5e9;padding:20px;border-radius:8px;"">
    <p style=""margin:0;color:#0369a1;""><strong>?? ?ang giao hàng</strong><br>D? ki?n giao trong 2-3 ngày làm vi?c.</p>
    {trackingHtml}
</div>

<div style=""text-align:center;margin-top:30px;"">
    <a href=""{_settings.WebsiteUrl}/don-hang/{order.Id}"" style=""display:inline-block;background:#1a1a1a;color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">Theo dõi ??n hàng</a>
</div>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildOrderDeliveredEmail(Order order, string customerName)
        {
            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">Giao hàng thành công! ?</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">??n hàng <strong>#ORD{order.Id:D6}</strong> ?ã ???c giao thành công.</p>

<div style=""background:#dcfce7;border-left:4px solid #22c55e;padding:20px;border-radius:8px;"">
    <p style=""margin:0;color:#166534;""><strong>? Hoàn thành</strong><br>C?m ?n b?n ?ã mua s?m t?i SPORTS Vietnam! Chúng tôi r?t mong nh?n ???c ?ánh giá c?a b?n.</p>
</div>

<div style=""text-align:center;margin-top:30px;"">
    <a href=""{_settings.WebsiteUrl}/san-pham"" style=""display:inline-block;background:#1a1a1a;color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">Ti?p t?c mua s?m</a>
</div>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildOrderCancelledEmail(Order order, string customerName, string? reason)
        {
            var reasonHtml = !string.IsNullOrEmpty(reason) ? $"<p style=\"margin:8px 0 0;\">Lý do: {reason}</p>" : "";

            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">??n hàng ?ã h?y ?</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">??n hàng <strong>#ORD{order.Id:D6}</strong> ?ã b? h?y.</p>

<div style=""background:#fee2e2;border-left:4px solid #ef4444;padding:20px;border-radius:8px;"">
    <p style=""margin:0;color:#991b1b;""><strong>? ?ã h?y</strong></p>
    {reasonHtml}
</div>

<p style=""color:#6b7280;margin-top:20px;"">N?u b?n ?ã thanh toán tr??c, ti?n s? ???c hoàn trong 5-7 ngày làm vi?c.</p>

<div style=""text-align:center;margin-top:30px;"">
    <a href=""{_settings.WebsiteUrl}/san-pham"" style=""display:inline-block;background:#1a1a1a;color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">Ti?p t?c mua s?m</a>
</div>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildAbandonedCartEmail(string customerName, List<CartItemEmailModel> items)
        {
            var itemsHtml = string.Join("", items.Select(item => $@"
<tr><td style=""padding:12px;border-bottom:1px solid #e5e7eb;"">
    <table><tr>
        <td><img src=""{item.ImageUrl}"" style=""width:60px;height:60px;object-fit:cover;border-radius:8px;""></td>
        <td style=""padding-left:12px;"">
            <p style=""margin:0;font-weight:600;"">{item.ProductName}</p>
            <p style=""margin:4px 0 0;color:#6b7280;font-size:12px;"">{item.VariantInfo}</p>
        </td>
        <td style=""text-align:right;font-weight:600;"">{item.Total:N0}?</td>
    </tr></table>
</td></tr>"));

            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">B?n quên gì ?ó! ??</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">Xin chào <strong>{customerName}</strong>, b?n còn s?n ph?m trong gi? hàng!</p>

<table width=""100%"" style=""margin-bottom:24px;"">{itemsHtml}</table>

<div style=""text-align:center;"">
    <a href=""{_settings.WebsiteUrl}/gio-hang"" style=""display:inline-block;background:#1a1a1a;color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">Quay l?i gi? hàng</a>
</div>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildAccountActivationEmail(string customerName, string activationLink)
        {
            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">Kích ho?t tài kho?n ??</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">Xin chào <strong>{customerName}</strong>, click vào nút bên d??i ?? kích ho?t tài kho?n.</p>

<div style=""text-align:center;margin:30px 0;"">
    <a href=""{activationLink}"" style=""display:inline-block;background:#22c55e;color:white;padding:16px 40px;border-radius:8px;text-decoration:none;font-weight:600;font-size:16px;"">Kích ho?t tài kho?n</a>
</div>

<p style=""color:#9ca3af;font-size:12px;text-align:center;"">Liên k?t h?t h?n sau 24 gi?.</p>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildPasswordResetEmail(string customerName, string resetLink)
        {
            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">??t l?i m?t kh?u ??</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">Xin chào <strong>{customerName}</strong>, click vào nút bên d??i ?? ??t l?i m?t kh?u.</p>

<div style=""text-align:center;margin:30px 0;"">
    <a href=""{resetLink}"" style=""display:inline-block;background:#f59e0b;color:white;padding:16px 40px;border-radius:8px;text-decoration:none;font-weight:600;font-size:16px;"">??t l?i m?t kh?u</a>
</div>

<div style=""background:#fef3c7;border-left:4px solid #f59e0b;padding:16px;border-radius:8px;"">
    <p style=""margin:0;color:#92400e;font-size:13px;"">?? Liên k?t ch? có hi?u l?c trong 1 gi?. N?u b?n không yêu c?u ??t l?i m?t kh?u, vui lòng b? qua email này.</p>
</div>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildWelcomeEmail(string customerName)
        {
            var content = $@"
<h2 style=""color:#1a1a1a;margin:0 0 10px;font-size:24px;"">Chào m?ng ??n v?i SPORTS Vietnam! ??</h2>
<p style=""color:#6b7280;margin:0 0 30px;"">Xin chào <strong>{customerName}</strong>, c?m ?n b?n ?ã tham gia c?ng ??ng SPORTS Vietnam!</p>

<div style=""background:linear-gradient(135deg,#f0fdf4 0%,#dcfce7 100%);border-radius:12px;padding:24px;text-align:center;margin-bottom:30px;"">
    <p style=""margin:0 0 8px;font-size:18px;color:#166534;"">?? ?u ?ãi chào m?ng</p>
    <p style=""margin:0;font-size:32px;font-weight:700;color:#166534;"">GI?M 10%</p>
    <p style=""margin:8px 0 0;color:#6b7280;font-size:14px;"">Mã: <strong style=""color:#166534;"">WELCOME10</strong></p>
</div>

<div style=""text-align:center;"">
    <a href=""{_settings.WebsiteUrl}/san-pham"" style=""display:inline-block;background:#1a1a1a;color:white;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;"">B?t ??u mua s?m</a>
</div>";

            return GetBaseTemplate().Replace("{{CONTENT}}", content);
        }

        private string BuildOrderItemsHtml(Order order)
        {
            var sb = new StringBuilder("<table width=\"100%\" style=\"border-collapse:collapse;\">");
            foreach (var item in order.OrderDetails ?? new List<OrderDetail>())
            {
                var productName = item.ProductVariant?.Product?.Name ?? "S?n ph?m";
                var variantInfo = $"{item.ProductVariant?.Color} - Size {item.ProductVariant?.Size}";
                var imageUrl = item.ProductVariant?.ImageUrl ?? "/images/no-image.svg";
                
                sb.Append($@"
<tr><td style=""padding:12px;border-bottom:1px solid #e5e7eb;"">
    <table><tr>
        <td><img src=""{_settings.WebsiteUrl}{imageUrl}"" style=""width:60px;height:60px;object-fit:cover;border-radius:8px;border:1px solid #e5e7eb;""></td>
        <td style=""padding-left:12px;"">
            <p style=""margin:0;font-weight:600;color:#1a1a1a;font-size:14px;"">{productName}</p>
            <p style=""margin:4px 0 0;color:#6b7280;font-size:12px;"">{variantInfo} × {item.Quantity}</p>
        </td>
        <td style=""text-align:right;font-weight:600;color:#1a1a1a;"">{(item.Quantity * item.UnitPrice):N0}?</td>
    </tr></table>
</td></tr>");
            }
            sb.Append("</table>");
            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Product model for promotion campaigns
    /// </summary>
    public class PromotionProduct
    {
        public string Name { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal OriginalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string ProductUrl { get; set; } = "";
    }
}
