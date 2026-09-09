using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using ShoesEcommerce.Data;
using ShoesEcommerce.Services.Interfaces;
using ShoesEcommerce.Services;
using ShoesEcommerce.Repositories.Interfaces;
using ShoesEcommerce.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using ShoesEcommerce.Middleware;
using ShoesEcommerce.ModelBinders;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using ShoesEcommerce.Services.Payment;
using ShoesEcommerce.Services.Options;
using ShoesEcommerce.Services.Interfaces;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ✅ Ensure Development configuration is loaded properly
if (builder.Environment.IsDevelopment())
{
    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();
    
    Console.WriteLine("📁 Configuration files loaded:");
    Console.WriteLine("   - appsettings.json");
    Console.WriteLine($"   - appsettings.{builder.Environment.EnvironmentName}.json");
}

// Enhanced logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Only add Debug logging in development
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

// Log current environment for debugging
Console.WriteLine($"🔧 Current Environment: {builder.Environment.EnvironmentName}");



// Configure detailed logging for specific components
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    options.MinLevel = builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information;
    options.AddFilter("Microsoft.EntityFrameworkCore", builder.Environment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning);
    options.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    options.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
});

// 🕐 Configure DateTime Culture and Localization
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en-US"),
        new CultureInfo("vi-VN")
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
});

// ✅ Get connection string - Check multiple sources
var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

if (string.IsNullOrEmpty(connectionString))
{
    // Try from configuration (appsettings.json or appsettings.Development.json)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

// Log connection string status (masked for security)
if (!string.IsNullOrEmpty(connectionString))
{
    var maskedConnStr = connectionString.Length > 50 
        ? connectionString.Substring(0, 30) + "***" + connectionString.Substring(connectionString.Length - 20)
        : "***configured***";
    Console.WriteLine($"✅ Database connection string found: {maskedConnStr}");
}
else
{
    Console.WriteLine("❌ WARNING: Database connection string is empty or not configured!");
    Console.WriteLine("   Please set DATABASE_CONNECTION_STRING environment variable");
    Console.WriteLine("   Or configure ConnectionStrings:DefaultConnection in appsettings.Development.json");
}

// Add DbContext with the connection string
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection string not configured. " +
            "Set DATABASE_CONNECTION_STRING environment variable or configure ConnectionStrings:DefaultConnection in appsettings.json/appsettings.Development.json");
    }
    
    options.UseNpgsql(connectionString);
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Register repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IQARepository, QARepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<CheckoutRepository>();
builder.Services.AddScoped<ShoesEcommerce.Repositories.Interfaces.IPaymentRepository, ShoesEcommerce.Repositories.PaymentRepository>();

// ✅ Register HttpClient for Supabase Storage
builder.Services.AddHttpClient("SupabaseStorage", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Allow longer timeout for large file uploads
});

// ✅ Register Supabase Storage Service using REST API (not S3)
builder.Services.Configure<SupabaseStorageOptions>(options =>
{
    // Bind from configuration section
    builder.Configuration.GetSection(SupabaseStorageOptions.SectionName).Bind(options);
    
    // Allow environment variables to override
    var envProjectUrl = Environment.GetEnvironmentVariable("SUPABASE_PROJECT_URL");
    var envServiceRoleKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
    var envBucketName = Environment.GetEnvironmentVariable("SUPABASE_BUCKET_NAME");

    if (!string.IsNullOrEmpty(envProjectUrl)) options.ProjectUrl = envProjectUrl;
    if (!string.IsNullOrEmpty(envServiceRoleKey)) options.ServiceRoleKey = envServiceRoleKey;
    if (!string.IsNullOrEmpty(envBucketName)) options.BucketName = envBucketName;

    // Log configuration
    Console.WriteLine("📦 Supabase Storage Configuration:");
    Console.WriteLine($"   - Project URL: {options.ProjectUrl}");
    Console.WriteLine($"   - Bucket: {options.BucketName}");
    Console.WriteLine($"   - Service Role Key: {(string.IsNullOrEmpty(options.ServiceRoleKey) ? "❌ NOT SET" : options.ServiceRoleKey.Substring(0, Math.Min(20, options.ServiceRoleKey.Length)) + "...")}");

    if (!string.IsNullOrEmpty(options.ProjectUrl) && !string.IsNullOrEmpty(options.ServiceRoleKey))
    {
        Console.WriteLine("✅ Supabase Storage configured successfully!");
    }
    else
    {
        Console.WriteLine("⚠️ Supabase Storage not fully configured!");
    }
});
builder.Services.AddScoped<IStorageService, SupabaseStorageService>();

// Register services (FileUploadService needs IStorageService - must be registered AFTER)
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerRegistrationService, CustomerRegistrationService>();
builder.Services.AddScoped<IStaffRegistrationService, StaffRegistrationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ShoesEcommerce.Services.ICommentService, ShoesEcommerce.Services.CommentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();

// Register VNPayService
builder.Services.AddScoped<IVnPayService, VnPayService>(); 

// ✅ Register Twilio SMS Service - Use proper binding from configuration
builder.Services.Configure<TwilioOptions>(options =>
{
    // First, bind from configuration section (appsettings.json / appsettings.Development.json)
    builder.Configuration.GetSection("Twilio").Bind(options);
    
    // Then allow environment variables to override (for production/CI environments)
    var envAccountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID");
    var envAuthToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN");
    var envPhoneNumber = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER");

    // Override with environment variables only if they are set
    if (!string.IsNullOrEmpty(envAccountSid)) options.AccountSid = envAccountSid;
    if (!string.IsNullOrEmpty(envAuthToken)) options.AuthToken = envAuthToken;
    if (!string.IsNullOrEmpty(envPhoneNumber)) options.PhoneNumber = envPhoneNumber;

    // Set defaults if not configured
    if (options.OtpLength == 0) options.OtpLength = 6;
    if (options.OtpExpirationMinutes == 0) options.OtpExpirationMinutes = 5;

    // Log final configuration
    Console.WriteLine("📱 Twilio SMS Configuration (after binding):");
    Console.WriteLine($"   - Account SID: {(string.IsNullOrEmpty(options.AccountSid) ? "❌ NOT SET" : options.AccountSid.Substring(0, Math.Min(8, options.AccountSid.Length)) + "***")}");
    Console.WriteLine($"   - Auth Token: {(string.IsNullOrEmpty(options.AuthToken) ? "❌ NOT SET" : "✅ SET")}");
    Console.WriteLine($"   - Phone Number: {(string.IsNullOrEmpty(options.PhoneNumber) ? "❌ NOT SET" : options.PhoneNumber)}");

    if (!string.IsNullOrEmpty(options.AccountSid) && !string.IsNullOrEmpty(options.AuthToken) && !string.IsNullOrEmpty(options.PhoneNumber))
    {
        Console.WriteLine($"✅ Twilio SMS Service configured successfully!");
    }
    else
    {
        Console.WriteLine("⚠️ Twilio SMS Service not fully configured.");
    }
});
builder.Services.AddScoped<ITwilioService, TwilioService>();

// Register Subiz Chat Service
builder.Services.Configure<SubizChatOptions>(builder.Configuration.GetSection(SubizChatOptions.SectionName));
builder.Services.AddScoped<ISubizChatService, SubizChatService>();

// Register Social Chat Options (Facebook, Zalo)
builder.Services.Configure<SocialChatOptions>(builder.Configuration.GetSection(SocialChatOptions.SectionName));

// ✅ Register Email Service
builder.Services.Configure<EmailSettings>(options =>
{
    // Bind from configuration
    builder.Configuration.GetSection(EmailSettings.SectionName).Bind(options);

    // Environment overrides
    var envProvider = Environment.GetEnvironmentVariable("EMAIL_PROVIDER");
    var envMailchimpKey = Environment.GetEnvironmentVariable("MAILCHIMP_API_KEY");
    var envMailchimpServer = Environment.GetEnvironmentVariable("MAILCHIMP_SERVER_PREFIX");
    var envMailchimpAudience = Environment.GetEnvironmentVariable("MAILCHIMP_AUDIENCE_ID");
    var envSmtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST");
    var envSmtpPort = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT");
    var envSenderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL");
    var envSenderName = Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME");
    var envUsername = Environment.GetEnvironmentVariable("EMAIL_USERNAME");
    var envPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
    var envWebsiteUrl = Environment.GetEnvironmentVariable("EMAIL_WEBSITE_URL");

    if (!string.IsNullOrEmpty(envProvider)) options.Provider = envProvider;
    if (!string.IsNullOrEmpty(envMailchimpKey)) options.MailchimpApiKey = envMailchimpKey;
    if (!string.IsNullOrEmpty(envMailchimpServer)) options.MailchimpServerPrefix = envMailchimpServer;
    if (!string.IsNullOrEmpty(envMailchimpAudience)) options.MailchimpAudienceId = envMailchimpAudience;
    if (!string.IsNullOrEmpty(envSmtpHost)) options.SmtpHost = envSmtpHost;
    if (!string.IsNullOrEmpty(envSmtpPort) && int.TryParse(envSmtpPort, out int port)) options.SmtpPort = port;
    if (!string.IsNullOrEmpty(envSenderEmail)) options.SenderEmail = envSenderEmail;
    if (!string.IsNullOrEmpty(envSenderName)) options.SenderName = envSenderName;
    if (!string.IsNullOrEmpty(envUsername)) options.Username = envUsername;
    if (!string.IsNullOrEmpty(envPassword)) options.Password = envPassword;
    if (!string.IsNullOrEmpty(envWebsiteUrl)) options.WebsiteUrl = envWebsiteUrl;

    Console.WriteLine("📧 Email Service Configuration:");
    Console.WriteLine($"   - Provider: {options.Provider}");
    Console.WriteLine($"   - Sender: {(string.IsNullOrEmpty(options.SenderEmail) ? "❌ NOT SET" : options.SenderEmail)}");
    
    if (options.Provider.ToLower() == "mailchimp")
    {
        Console.WriteLine($"   - Mailchimp API Key: {(string.IsNullOrEmpty(options.MailchimpApiKey) ? "❌ NOT SET" : "✅ SET")}");
        Console.WriteLine($"   - Mailchimp Server: {(string.IsNullOrEmpty(options.MailchimpServerPrefix) ? "❌ NOT SET" : options.MailchimpServerPrefix)}");
        Console.WriteLine($"   - Mailchimp Audience: {(string.IsNullOrEmpty(options.MailchimpAudienceId) ? "❌ NOT SET" : options.MailchimpAudienceId)}");
    }
    else
    {
        Console.WriteLine($"   - SMTP Host: {options.SmtpHost}:{options.SmtpPort}");
        Console.WriteLine($"   - SMTP Password: {(string.IsNullOrEmpty(options.Password) ? "❌ NOT SET" : "✅ SET")}");
    }
    
    Console.WriteLine($"   - Enabled: {options.Enabled}");

    if (!string.IsNullOrEmpty(options.SenderEmail) && 
        (!string.IsNullOrEmpty(options.MailchimpApiKey) || !string.IsNullOrEmpty(options.Password)))
    {
        Console.WriteLine("✅ Email Service configured successfully!");
    }
    else
    {
        Console.WriteLine("⚠️ Email Service not fully configured. Email notifications will be disabled.");
    }
});
builder.Services.AddScoped<IEmailService, EmailService>();

// Register VNPay Options
builder.Services.Configure<VnPayOptions>(options =>
{
    // Bind from configuration
    builder.Configuration.GetSection(VnPayOptions.SectionName).Bind(options);

    // Environment overrides
    var envTmnCode = Environment.GetEnvironmentVariable("VNPAY_TMNCODE");
    var envHashSecret = Environment.GetEnvironmentVariable("VNPAY_HASHSECRET");
    var envUrl = Environment.GetEnvironmentVariable("VNPAY_URL");
    var envReturnUrl = Environment.GetEnvironmentVariable("VNPAY_RETURNURL");

    if (!string.IsNullOrEmpty(envTmnCode)) options.TmnCode = envTmnCode;
    if (!string.IsNullOrEmpty(envHashSecret)) options.HashSecret = envHashSecret;
    if (!string.IsNullOrEmpty(envUrl)) options.Url = envUrl;
    if (!string.IsNullOrEmpty(envReturnUrl)) options.ReturnUrl = envReturnUrl;

    // Defaults
    if (string.IsNullOrEmpty(options.Url))
    {
        options.Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    }

    Console.WriteLine("💳 VNPay Configuration:");
    Console.WriteLine($"   - TmnCode: {(string.IsNullOrEmpty(options.TmnCode) ? "❌ NOT SET" : options.TmnCode.Substring(0, Math.Min(4, options.TmnCode.Length)) + "***")}");
    Console.WriteLine($"   - ReturnUrl: {(string.IsNullOrEmpty(options.ReturnUrl) ? "❌ NOT SET" : options.ReturnUrl)}");
    Console.WriteLine($"   - Url: {options.Url}");
});

// Register Tawk.to Chat Options
builder.Services.Configure<TawkToOptions>(builder.Configuration.GetSection(TawkToOptions.SectionName));

// Register PayPal HttpClient
builder.Services.AddHttpClient("PayPal", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Accept-Language", "en_US");
});

// ✅ Register PayPal Client - Make it optional (don't crash if not configured)
builder.Services.AddSingleton<ShoesEcommerce.Services.Payment.PayPalClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ShoesEcommerce.Services.Payment.PayPalClient>>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    
    var clientId = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID") 
                   ?? configuration["PayPalOptions:ClientId"];
    var clientSecret = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET") 
                       ?? configuration["PayPalOptions:ClientSecret"];
    var mode = Environment.GetEnvironmentVariable("PAYPAL_MODE") 
               ?? configuration["PayPalOptions:Mode"] 
               ?? "Sandbox";

    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        logger.LogWarning("⚠️ PayPal configuration missing. PayPal payment will not be available.");
        // Return a dummy client or throw - for now we throw to maintain existing behavior
        throw new InvalidOperationException("PayPal ClientId and ClientSecret must be configured");
    }

    logger.LogInformation("✅ PayPal Client initialized in {Mode} mode", mode);
    return new ShoesEcommerce.Services.Payment.PayPalClient(clientId, clientSecret, mode, logger, httpClientFactory);
});

// Register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddGoogle(googleOptions =>
    {
        var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
                             ?? builder.Configuration["Authentication:Google:ClientId"];
        var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") 
                                 ?? builder.Configuration["Authentication:Google:ClientSecret"];

        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            googleOptions.ClientId = googleClientId;
            googleOptions.ClientSecret = googleClientSecret;
            googleOptions.CallbackPath = "/signin-google";
            
            // Request additional scopes
            googleOptions.Scope.Add("email");
            googleOptions.Scope.Add("profile");
            
            Console.WriteLine("✅ Google OAuth configured successfully");
        }
        else
        {
            Console.WriteLine("⚠️ Google OAuth not configured. Set GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET environment variables or configure in appsettings.json");
            // Use placeholder values to avoid startup crash
            googleOptions.ClientId = "not-configured";
            googleOptions.ClientSecret = "not-configured";
        }
    });

// Configure MVC
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = false;
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("🚀 ShoesEcommerce application starting up...");
    logger.LogInformation("📍 Environment: {Environment}", app.Environment.EnvironmentName);

    // ✅ CRITICAL: Configure ForwardedHeaders FIRST for reverse proxy (Render, Heroku, etc.)
    // This MUST be before any other middleware to ensure correct scheme detection
    // Required for Google OAuth to generate correct redirect_uri with HTTPS
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
    };
    // Clear default limits to accept headers from any proxy (Render, Cloudflare, etc.)
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);
    logger.LogInformation("✅ ForwardedHeaders middleware configured for reverse proxy support");

    // Seed database
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Test connection first
        var canConnect = await context.Database.CanConnectAsync();
        logger.LogInformation("📊 Database connection test: {Status}", canConnect ? "SUCCESS" : "FAILED");
        
        if (canConnect)
        {
            await context.Database.EnsureCreatedAsync();
            await DataSeeder.SeedAllDataAsync(context);
            logger.LogInformation("✅ Database seeded successfully");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ An error occurred while connecting to or seeding the database");
    }

    // Configure HTTP pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        app.UseHsts();
        logger.LogInformation("🔒 Production error handling configured");
    }
    else
    {
        app.UseDeveloperExceptionPage();
        logger.LogInformation("🔧 Developer exception page enabled");
    }

    // ✅ Add Social Crawler support BEFORE authentication
    // This allows Facebook, Twitter, etc. to scrape OG tags without authentication issues
    app.UseSocialCrawlerSupport();

    app.UseSession();
    app.UseRequestLocalization();
    app.UseErrorLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    // Admin routes
    app.MapControllerRoute(name: "admin_product", pattern: "Admin/Product/{action=Index}/{id?}", defaults: new { controller = "AdminProduct" });
    app.MapControllerRoute(name: "admin_stock", pattern: "Admin/Stock/{action=Index}/{id?}", defaults: new { controller = "AdminStock" });
    app.MapControllerRoute(name: "admin_staff", pattern: "Admin/Staff/{action=Index}/{id?}", defaults: new { controller = "AdminStaff" });
    app.MapControllerRoute(name: "admin_order", pattern: "Admin/Order/{action=Index}/{id?}", defaults: new { controller = "AdminOrder" });
    app.MapControllerRoute(name: "admin_discount", pattern: "Admin/Discount/{action=Index}/{id?}", defaults: new { controller = "AdminDiscount" });
    app.MapControllerRoute(name: "admin_data", pattern: "Admin/Data/{action=Index}/{id?}", defaults: new { controller = "Data" });
    app.MapControllerRoute(name: "admin_default", pattern: "Admin/{action=Index}/{id?}", defaults: new { controller = "Admin" });

    // SEO routes
    app.MapControllerRoute(name: "product_detail_seo", pattern: "san-pham/{slug}", defaults: new { controller = "Product", action = "Details" });
    app.MapControllerRoute(name: "product_list_seo", pattern: "san-pham", defaults: new { controller = "Product", action = "Index" });
    app.MapControllerRoute(name: "discounted_products_seo", pattern: "khuyen-mai", defaults: new { controller = "Product", action = "DiscountedProducts" });
    app.MapControllerRoute(name: "cart_seo", pattern: "gio-hang", defaults: new { controller = "Cart", action = "Index" });
    app.MapControllerRoute(name: "checkout_seo", pattern: "thanh-toan", defaults: new { controller = "Checkout", action = "Index" });
    app.MapControllerRoute(name: "login_seo", pattern: "dang-nhap", defaults: new { controller = "Account", action = "Login" });
    app.MapControllerRoute(name: "register_seo", pattern: "dang-ky", defaults: new { controller = "Account", action = "Register" });
    app.MapControllerRoute(name: "orders_seo", pattern: "don-hang", defaults: new { controller = "Order", action = "Index" });
    app.MapControllerRoute(name: "order_detail_seo", pattern: "don-hang/{id}", defaults: new { controller = "Order", action = "Details" });
    app.MapControllerRoute(name: "favorites_seo", pattern: "yeu-thich", defaults: new { controller = "Favorite", action = "Index" });
    app.MapControllerRoute(name: "profile_seo", pattern: "tai-khoan", defaults: new { controller = "Account", action = "Profile" });
    app.MapControllerRoute(name: "order_tracking_seo", pattern: "tra-cuu-don-hang", defaults: new { controller = "Order", action = "Track" });

    // Default route
    app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapControllerRoute(name: "error", pattern: "Error/{statusCode?}", defaults: new { controller = "Error", action = "HandleErrorCode" });

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { 
        status = "healthy", 
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName,
        version = "1.0.0"
    }));

    logger.LogInformation("✅ ShoesEcommerce application started successfully");
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "💥 Application terminated unexpectedly during startup");
    throw;
}
