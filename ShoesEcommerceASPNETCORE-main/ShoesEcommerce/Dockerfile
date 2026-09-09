# Dockerfile for ShoesEcommerce ASP.NET Core 8.0 Application
# Optimized for Render deployment

# ============================================
# Stage 1: Build Stage
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["ShoesEcommerce.csproj", "."]
RUN dotnet restore "ShoesEcommerce.csproj"

# Copy everything else and build
COPY . .

# Remove any files with special characters that cause issues on Linux
RUN find . -name "*,*" -type f -delete 2>/dev/null || true
RUN find . -name "*'*" -type f -delete 2>/dev/null || true

# Rename Images to images for Linux consistency (case-sensitive filesystem)
RUN if [ -d "wwwroot/Images" ]; then \
        mkdir -p wwwroot/images && \
        cp -r wwwroot/Images/* wwwroot/images/ 2>/dev/null || true && \
        rm -rf wwwroot/Images; \
    fi

RUN dotnet build "ShoesEcommerce.csproj" -c Release -o /app/build

# ============================================
# Stage 2: Publish Stage
# ============================================
FROM build AS publish

# Create the output directory structure first
RUN mkdir -p /app/publish/wwwroot/images

RUN dotnet publish "ShoesEcommerce.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Ensure images directory exists with correct case after publish
RUN if [ -d "/app/publish/wwwroot/Images" ]; then \
        mkdir -p /app/publish/wwwroot/images && \
        cp -r /app/publish/wwwroot/Images/* /app/publish/wwwroot/images/ 2>/dev/null || true && \
        rm -rf /app/publish/wwwroot/Images; \
    fi

# ============================================
# Stage 3: Runtime Stage
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health check and other dependencies
RUN apt-get update && apt-get install -y \
    curl \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create directory for logs and ensure images directory exists
RUN mkdir -p /app/logs /app/wwwroot/images

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:10000
ENV TZ=Asia/Ho_Chi_Minh

# Expose the port Render expects
EXPOSE 10000

# Health check (must be on single line)
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 CMD curl --fail http://localhost:10000/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "ShoesEcommerce.dll"]
