# Environment Variables Configuration

## Overview

This project uses environment variables for sensitive configuration to prevent credentials from being committed to version control.

## Required Environment Variables

### Database Connection
```bash
DATABASE_CONNECTION_STRING="Host=your-host.supabase.com;Port=5432;Database=postgres;Username=your-username;Password=your-password;Pooling=true;"
```

### PayPal Configuration
```bash
PAYPAL_CLIENT_ID="your-paypal-client-id"
PAYPAL_CLIENT_SECRET="your-paypal-client-secret"
PAYPAL_MODE="Sandbox"  # or "Live" for production
```

### VNPay Configuration
```bash
VNPAY_TMN_CODE="your-vnpay-tmn-code"
VNPAY_HASH_SECRET="your-vnpay-hash-secret"
VNPAY_URL="https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
VNPAY_RETURN_URL="https://your-domain.com/Payment/VnPayReturn"
```

## Setup Methods

### Method 1: Local Development (Recommended)

1. Copy `appsettings.Development.json.template` to `appsettings.Development.json`
2. Fill in your actual values
3. This file is already in `.gitignore` and won't be committed

```bash
cp appsettings.Development.json.template appsettings.Development.json
```

### Method 2: Environment Variables (Windows)

Set environment variables in PowerShell:

```powershell
# Database
$env:DATABASE_CONNECTION_STRING = "Host=your-host.supabase.com;Port=5432;Database=postgres;Username=your-username;Password=your-password;Pooling=true;"

# PayPal
$env:PAYPAL_CLIENT_ID = "your-paypal-client-id"
$env:PAYPAL_CLIENT_SECRET = "your-paypal-client-secret"
$env:PAYPAL_MODE = "Sandbox"

# VNPay
$env:VNPAY_TMN_CODE = "your-vnpay-tmn-code"
$env:VNPAY_HASH_SECRET = "your-vnpay-hash-secret"
$env:VNPAY_RETURN_URL = "https://localhost:7085/Payment/VnPayReturn"
```

Or set permanently in System Properties ? Environment Variables.

### Method 3: Environment Variables (Linux/macOS)

Add to `~/.bashrc` or `~/.zshrc`:

```bash
export DATABASE_CONNECTION_STRING="Host=your-host.supabase.com;Port=5432;Database=postgres;Username=your-username;Password=your-password;Pooling=true;"
export PAYPAL_CLIENT_ID="your-paypal-client-id"
export PAYPAL_CLIENT_SECRET="your-paypal-client-secret"
export PAYPAL_MODE="Sandbox"
export VNPAY_TMN_CODE="your-vnpay-tmn-code"
export VNPAY_HASH_SECRET="your-vnpay-hash-secret"
export VNPAY_RETURN_URL="https://localhost:7085/Payment/VnPayReturn"
```

### Method 4: launchSettings.json (Development Only)

Add to `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "ShoesEcommerce": {
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DATABASE_CONNECTION_STRING": "your-connection-string",
        "PAYPAL_CLIENT_ID": "your-client-id",
        "PAYPAL_CLIENT_SECRET": "your-client-secret"
      }
    }
  }
}
```

### Method 5: Render/Production Deployment

In Render Dashboard ? Environment:

1. Go to your Web Service
2. Click "Environment"
3. Add each variable

| Key | Value |
|-----|-------|
| `DATABASE_CONNECTION_STRING` | Your Supabase connection string |
| `PAYPAL_CLIENT_ID` | Your PayPal Client ID |
| `PAYPAL_CLIENT_SECRET` | Your PayPal Client Secret |
| `PAYPAL_MODE` | `Live` |
| `VNPAY_TMN_CODE` | Your VNPay TMN Code |
| `VNPAY_HASH_SECRET` | Your VNPay Hash Secret |
| `VNPAY_RETURN_URL` | `https://your-app.onrender.com/Payment/VnPayReturn` |

## Configuration Priority

The application checks for configuration in this order:

1. **Environment Variables** (highest priority)
2. **appsettings.{Environment}.json** (e.g., appsettings.Development.json)
3. **appsettings.json** (lowest priority - contains empty/default values)

## Security Best Practices

1. ? **Never commit credentials** to Git
2. ? Use `appsettings.Development.json` for local development (already in .gitignore)
3. ? Use environment variables for production
4. ? Rotate credentials periodically
5. ? Use different credentials for Sandbox vs Production
6. ? Never share credentials in chat, email, or documentation

## Troubleshooting

### "Database connection string not configured"
- Check if `DATABASE_CONNECTION_STRING` environment variable is set
- Or check `appsettings.Development.json` has valid `ConnectionStrings:DefaultConnection`

### "PayPal ClientId and ClientSecret must be configured"
- Check if `PAYPAL_CLIENT_ID` and `PAYPAL_CLIENT_SECRET` are set
- Or check `appsettings.Development.json` has valid PayPal settings

### "VNPay configuration is missing"
- Check if `VNPAY_TMN_CODE` and `VNPAY_HASH_SECRET` are set
- Or check `appsettings.Development.json` has valid VNPay settings

## File Structure

```
ShoesEcommerce/
??? appsettings.json                    # ? Committed (empty sensitive values)
??? appsettings.Development.json        # ? NOT committed (contains real values)
??? appsettings.Development.json.template  # ? Committed (template for developers)
??? .gitignore                          # ? Excludes Development.json
??? ENV_SETUP.md                        # ? This documentation
```
