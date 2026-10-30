# Product Catalog Management API & Web Portal (.NET 8)

A production-ready ASP.NET Core 8.0 Web API and Razor/Bootstrap 5 web application for managing products with JWT authentication, search/filtering, in-memory caching, rate limiting, XSS/injection sanitization, Serilog structured logging, and xUnit automated unit tests.

## How to Run Locally (4 Simple Steps)

1. **Prerequisites**: Install the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (compatible with Windows, Linux, and macOS).
2. **Restore & Build**:
   ```bash
   dotnet restore && dotnet build
   ```
3. **Run Unit Tests & Code Coverage**:
   ```bash
   dotnet test --collect:"XPlat Code Coverage"
   ```
4. **Start the Application**:
   ```bash
   dotnet run --project src/ProductCatalog.Api
   ```
   Open `http://localhost:5000` (or the port shown in your terminal) for the Web UI, or `/swagger` for the interactive OpenAPI documentation.

## Architectural Overview & Key Features

- **Authentication & Authorization**: JWT (`Bearer`) token authentication via `/api/auth/register` and `/api/auth/login`. Product mutation endpoints (`POST`, `PUT`, `DELETE`) are protected with `[Authorize]`.
- **Product Management & Search**: Full async CRUD (`/api/products`) and multi-criteria filtering (`/api/products/search?name=...&minPrice=...&maxPrice=...`).
- **Architecture & Design Pattern**: Clean separation of concerns using the **Async Repository Pattern** (`IProductRepository`, `IUserRepository`) backed by Entity Framework Core (SQLite for zero-config cross-platform local persistence).
- **Caching**: `IMemoryCache` caches `GET /api/products` responses and automatically invalidates the cache upon product creation, update, or deletion.
- **Security Measures**:
  - **Rate Limiting**: Fixed-window rate limiter (`CriticalEndpoints` policy) applied to authentication and write endpoints to prevent brute-force and abuse.
  - **Data Validation & XSS Prevention**: Model validation via Data Annotations (`[Required]`, `[StringLength]`, `[Range]`, `[RegularExpression]`) paired with `InputSanitizer` to strip and HTML-encode dangerous script/SQL injection patterns.
- **Logging & Global Exception Handling**: Structured console and rolling-file logging via **Serilog** (`logs/product-catalog-.log`) and centralized `GlobalExceptionMiddleware` returning standardized JSON error payloads.

## Assumptions Made

1. **Read vs. Write Access**: Viewing and searching products (`GET /api/products` and `GET /api/products/search`) are publicly accessible to allow catalog browsing, whereas creating, updating, and deleting products (`POST`, `PUT`, `DELETE`) require a valid JWT Bearer token.
2. **Zero-Config Local Database**: SQLite (`product_catalog.db`) is used by default so evaluators on Linux, macOS, or Windows can run the app in under 5 steps without setting up an external SQL Server or Redis container.
3. **Default Seeded Credentials**: On startup, `DbSeeder` automatically creates a default administrator account (`username`: `admin`, `password`: `Admin@123`) and three sample products to streamline functional evaluation.
