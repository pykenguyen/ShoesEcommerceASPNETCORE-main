using Microsoft.Extensions.Options;
using ShoesEcommerce.Services.Options;
using ShoesEcommerce.ViewModels.Payment;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ShoesEcommerce.Services.Payment
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayOptions _options;
        private readonly ILogger<VnPayService> _logger;

        public VnPayService(IOptions<VnPayOptions> options, ILogger<VnPayService> logger)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <summary>
        /// Ensure required option is present, otherwise throw with guidance
        /// </summary>
        private string Require(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _logger.LogError("VNPay {Name} is missing", name);
                throw new InvalidOperationException($"VNPay {name} is not configured. Please set VNPAY_{name.ToUpper()} environment variable or VNPay:{name} in appsettings.");
            }

            return value;
        }

        /// <summary>
        /// Get client IP address, ensuring IPv4 format for VNPay
        /// </summary>
        private string GetIpAddress(HttpContext context)
        {
            try
            {
                var ipAddress = context.Connection.RemoteIpAddress;
                
                if (ipAddress != null)
                {
                    // If IPv6, try to get IPv4
                    if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        ipAddress = Dns.GetHostEntry(ipAddress).AddressList
                            .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    }
                    
                    if (ipAddress != null)
                    {
                        return ipAddress.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting IP address, using fallback");
            }
            
            return "127.0.0.1";
        }

        private static string VnPayEncode(string input)
        {
            return WebUtility.UrlEncode(input ?? string.Empty) ?? string.Empty;
        }

        public string CreatePaymentUrl(int orderId, decimal amount, HttpContext context)
        {
            var vnp_TmnCode = Require(_options.TmnCode, "TmnCode");
            var vnp_HashSecret = Require(_options.HashSecret, "HashSecret");
            var vnp_Url = string.IsNullOrEmpty(_options.Url)
                ? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
                : _options.Url;
            var vnp_ReturnUrl = Require(_options.ReturnUrl, "ReturnUrl");

            // Log configuration for debugging
            _logger.LogInformation("VNPay Config - TmnCode: {TmnCode}, Url: {Url}, ReturnUrl: {ReturnUrl}",
                string.IsNullOrEmpty(vnp_TmnCode) ? "NOT SET" : vnp_TmnCode.Substring(0, Math.Min(4, vnp_TmnCode.Length)) + "***",
                vnp_Url,
                vnp_ReturnUrl);

            var ipAddr = GetIpAddress(context);
            
            // Convert amount to VND (multiply by 100 as VNPay requires)
            var vnpAmount = ((long)(amount * 100)).ToString();
            var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            
            // ✅ IMPROVED: Use cleaner TxnRef format that maps to Invoice Number
            // Format: INV-{OrderId}-{Date}-{UniqueCode}
            // This makes "Số hóa đơn" on VNPay dashboard display nicely
            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            var uniqueCode = GenerateShortUniqueCode(); // 4 character alphanumeric
            var invoiceNumber = $"INV-{orderId}-{dateStr}";
            var txnRef = $"{invoiceNumber}-{uniqueCode}"; // Example: INV-63-20260108-A1B2
            
            // ✅ ENHANCED: Include descriptive order info for VNPay portal
            var orderInfo = $"SPORTS Vietnam - Don hang #{orderId} - {invoiceNumber}";

            _logger.LogInformation("Creating VNPay URL - OrderId: {OrderId}, Amount: {Amount} VND, TxnRef: {TxnRef}, Invoice: {Invoice}, IP: {IP}",
                orderId, amount, txnRef, invoiceNumber, ipAddr);

            // Build query parameters in sorted order
            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            vnp_Params.Add("vnp_Version", "2.1.0");
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_TmnCode", vnp_TmnCode);
            vnp_Params.Add("vnp_Amount", vnpAmount);
            vnp_Params.Add("vnp_CreateDate", createDate);
            vnp_Params.Add("vnp_CurrCode", "VND");
            vnp_Params.Add("vnp_IpAddr", ipAddr);
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_OrderInfo", orderInfo);
            vnp_Params.Add("vnp_OrderType", "billpayment");
            vnp_Params.Add("vnp_ReturnUrl", vnp_ReturnUrl);
            vnp_Params.Add("vnp_TxnRef", txnRef);
            // ✅ Add expire date (15 minutes from now)
            vnp_Params.Add("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            // Build query string and hash data
            var queryBuilder = new StringBuilder();
            var hashDataBuilder = new StringBuilder();

            foreach (var kv in vnp_Params.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                var key = VnPayEncode(kv.Key);
                var value = VnPayEncode(kv.Value);

                // Query string uses URL encoding
                queryBuilder.Append(key);
                queryBuilder.Append("=");
                queryBuilder.Append(value);
                queryBuilder.Append("&");

                // Hash data uses the same encoding
                hashDataBuilder.Append(key);
                hashDataBuilder.Append("=");
                hashDataBuilder.Append(value);
                hashDataBuilder.Append("&");
            }

            // Remove trailing &
            var query = queryBuilder.ToString().TrimEnd('&');
            var hashData = hashDataBuilder.ToString().TrimEnd('&');

            // Calculate HMAC SHA512
            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, hashData);

            // Build final URL
            var paymentUrl = $"{vnp_Url}?{query}&vnp_SecureHashType=HMACSHA512&vnp_SecureHash={vnp_SecureHash}";

            _logger.LogInformation("VNPay payment URL created successfully for order {OrderId}, TxnRef: {TxnRef}", orderId, txnRef);
            _logger.LogDebug("VNPay URL: {Url}", paymentUrl);

            return paymentUrl;
        }

        public VnPayReturnModel ProcessReturn(IQueryCollection queryParams)
        {
            var vnp_HashSecret = Require(_options.HashSecret, "HashSecret");

            var vnp_SecureHash = queryParams["vnp_SecureHash"].ToString();

            _logger.LogInformation("Processing VNPay return - SecureHash: {Hash}", 
                string.IsNullOrEmpty(vnp_SecureHash) ? "MISSING" : vnp_SecureHash.Substring(0, Math.Min(10, vnp_SecureHash.Length)) + "...");

            var response = new VnPayReturnModel
            {
                Vnp_TransactionNo = queryParams["vnp_TransactionNo"],
                Vnp_OrderInfo = queryParams["vnp_OrderInfo"],
                Vnp_ResponseCode = queryParams["vnp_ResponseCode"],
                Vnp_TxnRef = queryParams["vnp_TxnRef"],
                Vnp_Amount = queryParams["vnp_Amount"],
                Vnp_SecureHash = vnp_SecureHash
            };

            // Build hash data from response (excluding vnp_SecureHash and vnp_SecureHashType)
            var vnp_Params = new SortedList<string, string>(new VnPayCompare());
            foreach (var key in queryParams.Keys)
            {
                var keyLower = key.ToLower();
                if (keyLower != "vnp_securehash" && keyLower != "vnp_securehashtype" && !string.IsNullOrEmpty(queryParams[key]))
                {
                    vnp_Params.Add(key, queryParams[key].ToString());
                }
            }

            var hashDataBuilder = new StringBuilder();
            foreach (var kv in vnp_Params)
            {
                var key = VnPayEncode(kv.Key);
                var value = VnPayEncode(kv.Value);

                hashDataBuilder.Append(key);
                hashDataBuilder.Append("=");
                hashDataBuilder.Append(value);
                hashDataBuilder.Append("&");
            }
            var hashData = hashDataBuilder.ToString().TrimEnd('&');

            var expectedHash = HmacSHA512(vnp_HashSecret, hashData);
            response.IsSuccess = expectedHash.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
            
            _logger.LogInformation("VNPay return processed: TxnRef={TxnRef}, ResponseCode={ResponseCode}, IsSuccess={IsSuccess}",
                response.Vnp_TxnRef, response.Vnp_ResponseCode, response.IsSuccess);

            if (!response.IsSuccess)
            {
                _logger.LogWarning("VNPay hash validation failed. Expected: {Expected}, Got: {Got}",
                    expectedHash.Substring(0, 20) + "...", 
                    vnp_SecureHash?.Substring(0, Math.Min(20, vnp_SecureHash?.Length ?? 0)) + "...");
            }

            return response;
        }

        /// <summary>
        /// Compute HMAC SHA512 hash
        /// </summary>
        private static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var b in hashValue)
                {
                    hash.Append(b.ToString("x2"));
                }
            }
            
            return hash.ToString();
        }

        /// <summary>
        /// Generate a short unique alphanumeric code (4 characters) for TxnRef uniqueness
        /// </summary>
        private static string GenerateShortUniqueCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 4)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
    
    /// <summary>
    /// Comparer for VNPay parameter sorting (ASCII order)
    /// </summary>
    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
}