  # 🥿 ShoesEcommerce ASP.NET Core MVC

  [![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
  [![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-8.0%20MVC-5C2D91?logo=dotnet&logoColor=white)](https://learn.microsoft.com/aspnet/core)
  [![Entity Framework Core](https://img.shields.io/badge/Entity%20Framework-Core%208-6DB33F?logo=nuget&logoColor=white)](https://learn.microsoft.com/ef/core)
  [![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-CC2927?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
  [![Architecture](https://img.shields.io/badge/Architecture-Layered%20%7C%20Repository%20%2B%20Service-0A66C2)](#-architecture--patterns)
  [![DI](https://img.shields.io/badge/DI-Interfaces%20%7C%20Singleton%20%7C%20Scoped-00A98F)](#-core-design--patterns)
  [![Status](https://img.shields.io/badge/Payments-COD-2E7D32)](#-cart-checkout-orders--payment-cod)

  A professionally structured e‑commerce web application for selling shoes, built with ASP.NET Core 8.0 MVC, Entity Framework Core 8, and SQL Server. The project follows clean, layered architecture with interface-driven design, Repository and Service patterns, View Models for presentation, and careful use of the Singleton design pattern. It implements a complete feature set for real-world scenarios: full product CRUD, authentication and authorization, cart, orders, and Cash‑On‑Delivery payments.

  ---

  ## ✨ Introduction

  This project showcases best practices for building maintainable, testable, and production-ready ASP.NET Core 8 MVC applications:

  - Clean separation of concerns: Controllers → Services → Repositories
  - Interface-based dependency injection for flexibility and unit testing
  - View Models to decouple UI from domain entities
  - EF Core used for data access and migrations against SQL Server
  - Real e‑commerce features: product catalog, cart, orders, COD payments
  - Role-based authentication/authorization for admin and customer areas

  Perfect for portfolio reviews and technical interviews.

  ---

  ## 🌟 Key Highlights 

  - Production-minded architecture with SOLID principles
  - Secure login/registration and role-based access (Admin/Customer)
  - Fully functional CRUD for products with validation
  - Shopping cart and transactional order flow
  - Payments via Cash On Delivery (extensible design for more gateways)
  - EF Core migrations and SQL Server integration
  - Clean code with clear boundaries and testable services

  ---

  ## 🧱 Features

  - ✅ Product management: Create, Read, Update, Delete
  - 🔒 Authentication & Authorization (role-based)
  - 🛒 Shopping cart (session or persisted)
  - 📦 Orders: checkout, order items, status tracking
  - 💳 Payment: Cash On Delivery (COD)
  - 🧩 Repository & Service layers with interfaces
  - 🧰 View Models for forms and pages
  - 🗄️ SQL Server with EF Core migrations

  ---

  ## 🏗️ Architecture & Patterns

  - MVC presentation (Controllers, Views/Razor)
  - Service layer encapsulating business rules
  - Repository pattern for data access with EF Core DbContext
  - Interface-driven DI for services and repositories
  - Singleton pattern applied to stateless, thread-safe utilities only
  - View Models to shape UI data (separate from EF entities)

  Architecture overview (Mermaid-free, renders everywhere):
  ```
  Client (Razor Views)
          |
    Controllers  ---- uses ---->  View Models
          |
      Services  (Business Logic)
          |
  Repositories  (EF Core)
          |
      SQL Server
  ```

  ---

  ## 🧰 Tech Stack

  - Language: C# (ASP.NET Core 8 MVC)
  - Data: Entity Framework Core 8 + SQL Server
  - UI: Razor Views (HTML/CSS/JS)
  - Auth: ASP.NET Core Identity or cookie-based auth with roles
  - DI: Built-in .NET dependency injection

  ---

  ## 📁 Project Structure (high level)

  ```
  /Controllers
  /Models              # Domain/Entity models (EF Core)
  /ViewModels          # UI-specific DTOs
  /Data                # DbContext, seeders
  /Repositories        # Interfaces + EF Core implementations
  /Services            # Business logic interfaces + implementations
  /Views               # Razor views
  /Migrations          # EF Core migrations
  /wwwroot             # Static assets
  appsettings*.json    # Configuration (connection strings, etc.)
  ```

  ---

  ## 🚀 Getting Started

  ### Prerequisites
  - .NET SDK 8.0 installed (Target Framework: net8.0)
  - SQL Server (LocalDB, SQL Express, Docker, or remote)
  - Optional: EF Core tools: `dotnet tool install --global dotnet-ef`

  ### 1) Clone
  ```bash
  git clone https://github.com/PhungGiaDat/ShoesEcommerceASPNETCORE.git
  cd ShoesEcommerceASPNETCORE
  ```

  ### 2) Configure database
  Update `appsettings.json` (or `appsettings.Development.json`):
  ```json
  {
    "ConnectionStrings": {
      "DefaultConnection": "Server=.;Database=ShoesEcommerceDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
  }
  ```

  ### 3) Database and EF Core migrations
  ```bash
  # Create migration (if needed)
  dotnet ef migrations add InitialCreate --project ./YourProject.csproj --startup-project ./YourProject.csproj

  # Apply migrations
  dotnet ef database update --project ./YourProject.csproj --startup-project ./YourProject.csproj
  ```
  Replace `YourProject.csproj` with the actual project file path.

  ### 4) Run
  ```bash
  dotnet run --project ./YourProject.csproj
  ```
  Open your browser at the printed URL (e.g., http://localhost:5000 or https://localhost:5001).

  ---

  ## 🧠 Core Design & Patterns

  - Interfaces + DI
    - `IProductRepository`, `IProductService`, `IOrderService`, etc.
    - Registered via `AddScoped` for DbContext-dependent types.
  - Repository pattern
    - Encapsulates EF Core queries and CRUD operations.
  - Service layer
    - Holds business logic, validation, orchestration, and transactions.
  - View Models
    - `ProductCreateViewModel`, `ProductEditViewModel`, `CheckoutViewModel` to keep views decoupled from entities.
  - Singleton usage
    - Only for stateless, thread-safe utilities (e.g., simple in-memory cache, config readers).

  Example DI setup (Program.cs):
  ```csharp
  builder.Services.AddDbContext<ApplicationDbContext>(opts =>
      opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

  builder.Services.AddScoped<IProductRepository, ProductRepository>();
  builder.Services.AddScoped<IProductService, ProductService>();
  builder.Services.AddScoped<IOrderRepository, OrderRepository>();
  builder.Services.AddScoped<IOrderService, OrderService>();

  // Example: stateless utility as Singleton
  builder.Services.AddSingleton<IAppClock, SystemClock>();
  ```

  ---

  ## 🔐 Authentication & Authorization

  - User registration/login/logout using ASP.NET Core Identity or cookie auth
  - Role-based access control:
    - Customers: browse, cart, checkout, order history
    - Admins: manage products, view/manage orders
  - Controller protection with `[Authorize]` and `[Authorize(Roles = "Admin")]`

  ---

  ## 🛍️ Product CRUD

  - Admin can create, edit, delete, and list products
  - Typical fields: Name, Description, Price, Stock, Category, Image
  - Server-side validation via DataAnnotations
  - Repository → Service ensures rules and transaction boundaries

  ---

  ## 🧾 Cart, Checkout, Orders & Payment (COD)

  - Cart stored in session or database and tied to user account
  - Checkout collects shipping info and payment method
  - COD flow:
    - Order saved with `PaymentMethod = COD` and `PaymentStatus = Pending`
    - Stock adjustments and order items saved atomically
    - Admin updates order status upon delivery

  ---

  ## 🖼️ Screenshots 

  - Product list/details
    <img width="813" height="422" alt="image" src="https://github.com/user-attachments/assets/58f73fea-ce1f-4152-ae0e-ef2b5cb57cb6" />
  - Admin product CRUD
    <img width="898" height="473" alt="image" src="https://github.com/user-attachments/assets/f67246e2-c7e3-45fb-b629-68e189f63018" />
  - Admin adding product
  - <img width="941" height="486" alt="image" src="https://github.com/user-attachments/assets/0ac477f8-5168-443a-8cf4-706a9aa8a040" />
  - Cart and checkout
    Cart
<img width="941" height="500" alt="image" src="https://github.com/user-attachments/assets/13b1aea1-8631-4524-8c46-675f20ebaab1" />
  Checkout
<img width="912" height="472" alt="image" src="https://github.com/user-attachments/assets/a7ac2f54-adc4-417c-8ff3-1443b5fc2067" />
<img width="870" height="472" alt="image" src="https://github.com/user-attachments/assets/c0ff7f47-5abc-4d20-a841-1bf96d79353f" />
  - Order history
  <img width="955" height="498" alt="image" src="https://github.com/user-attachments/assets/3989fefd-ae8c-49b7-8c1e-1a386cba7515" />
<img width="958" height="472" alt="image" src="https://github.com/user-attachments/assets/7cbc27a2-8fa2-43c6-b629-b7df519c4cc4" />
  - Paging, sorting, filtering
  <img width="941" height="498" alt="image" src="https://github.com/user-attachments/assets/71070e36-1f99-4c57-9cab-7742e4c29cde" />
  - Admin dashboards and report
    <img width="846" height="450" alt="image" src="https://github.com/user-attachments/assets/51a74db5-72ce-44c7-a49a-4350c6c8b94a" />


  This visually communicates the UX to HR quickly.

  ---

  ## 🧭 Roadmap Future

  - Online payment gateways (Stripe, PayPal, VNPay)
  - Wishlists and product reviews
  - Caching strategies (e.g., MemoryCache/Redis)

  ---

  ## 🤝 Contribution

  - Fork the repo
  - Create a feature branch: `git checkout -b feature/your-feature`
  - Commit and push
  - Open a Pull Request with a clear description and screenshots if UI changes

  ---

  ## 📄 License

  Specify a license for the repository (e.g., MIT) by adding a `LICENSE` file to the root.

  ---

  ## 📬 Contact

  - Author: Phung Gia Dat
  - GitHub: [PhungGiaDat](https://github.com/PhungGiaDat)
  - For questions or support, please open an Issue.

  ---
  Made with ❤️ using ASP.NET Core 8, EF Core 8, and SQL Server.
