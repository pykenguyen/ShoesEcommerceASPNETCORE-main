# ?? DEPLOYMENT GUIDE - PayPal Payment Sync Fix

## ?? **OVERVIEW**

This guide walks you through deploying the PayPal payment synchronization fix to your production environment.

### **What This Fix Does:**
```
? Adds TransactionId column to Payments table
? Updates PaymentRepository to save transaction IDs
? Moves CreatePayPalOrderRequest to ViewModels
? Enables complete payment tracking
? Allows transaction verification
```

---

## ?? **PRE-DEPLOYMENT CHECKLIST**

### **1. Verify Current State:**
```bash
# Check build status
dotnet build

# Expected: Build succeeded
```

### **2. Backup Database (IMPORTANT!):**
```sql
-- In Supabase SQL Editor:
-- Create backup of Payments table
CREATE TABLE "Payments_Backup_20251211" AS 
SELECT * FROM "Payments";

-- Verify backup
SELECT COUNT(*) FROM "Payments_Backup_20251211";
```

### **3. Check Current Payments:**
```sql
-- See current state
SELECT "Id", "OrderId", "Method", "Status", "PaidAt" 
FROM "Payments" 
ORDER BY "Id" DESC 
LIMIT 10;

-- Count by status
SELECT "Status", COUNT(*) 
FROM "Payments" 
GROUP BY "Status";
```

---

## ?? **STEP-BY-STEP DEPLOYMENT**

### **STEP 1: Apply Database Migration**

#### **Option A: Using Supabase SQL Editor (RECOMMENDED)**

1. **Open Supabase Dashboard**
   - Go to https://supabase.com/dashboard
   - Select your project
   - Click "SQL Editor" in sidebar

2. **Run Migration Script**
   ```sql
   -- Copy and paste from: SUPABASE_ADD_TRANSACTIONID_MIGRATION.sql
   
   -- Quick version:
   ALTER TABLE "Payments" ADD COLUMN "TransactionId" TEXT;
   
   CREATE INDEX IF NOT EXISTS idx_payments_transactionid 
   ON "Payments" ("TransactionId") 
   WHERE "TransactionId" IS NOT NULL;
   ```

3. **Verify Column Added**
   ```sql
   SELECT column_name, data_type, is_nullable
   FROM information_schema.columns
   WHERE table_name = 'Payments'
   AND column_name = 'TransactionId';
   
   -- Expected result:
   -- TransactionId | text | YES
   ```

#### **Option B: Using Entity Framework (if preferred)**

```bash
# In terminal (PowerShell):
cd "E:\University\Application Development\Final Project MVC\ShoesEcommerce"

# Install EF tools if not installed
dotnet tool install --global dotnet-ef

# Apply migration
dotnet ef database update

# Verify
dotnet ef migrations list
```

---

### **STEP 2: Deploy Application Code**

#### **1. Stop Application**
```bash
# If running locally:
# Press Ctrl+C in terminal

# If running as service:
# Stop the service
```

#### **2. Build Application**
```bash
cd "E:\University\Application Development\Final Project MVC\ShoesEcommerce"

# Clean build
dotnet clean
dotnet build --configuration Release

# Expected output:
# Build succeeded.
```

#### **3. Run Tests (if you have any)**
```bash
dotnet test

# If no tests:
# Skip this step
```

#### **4. Start Application**
```bash
# Development:
dotnet run

# Production:
dotnet run --configuration Release
```

---

### **STEP 3: Verify Deployment**

#### **Test 1: Check Application Starts**
```
? Application starts without errors
? No database connection errors
? Logs show successful startup
```

#### **Test 2: Database Structure**
```sql
-- In Supabase SQL Editor:
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'Payments'
ORDER BY ordinal_position;

-- Expected columns:
-- Id, OrderId, Method, Status, PaidAt, TransactionId ?
```

#### **Test 3: Place Test PayPal Order**

1. **Create Test Order:**
   ```
   - Add product to cart
   - Go to checkout
   - Select PayPal payment
   - Use PayPal Sandbox credentials
   ```

2. **Approve Payment:**
   ```
   - Log in to PayPal Sandbox
   - Approve payment
   - Get redirected back
   ```

3. **Verify in Database:**
   ```sql
   -- Check latest payment
   SELECT 
       "Id",
       "OrderId",
       "Method",
       "Status",
       "PaidAt",
       "TransactionId"
   FROM "Payments"
   ORDER BY "Id" DESC
   LIMIT 1;
   
   -- Expected:
   -- Status = 'Paid' ?
   -- PaidAt = timestamp ?
   -- TransactionId = 'PAYID-XXXXX' ?
   ```

4. **Check Logs:**
   ```
   Look for:
   ? "PayPal order created successfully"
   ? "Capturing PayPal order"
   ? "Payment updated for order X: Status=Paid, TransactionId=PAYID-XXXXX"
   ```

---

## ?? **VERIFICATION QUERIES**

### **Quick Health Check:**
```sql
-- 1. Check recent payments
SELECT 
    "Id",
    "OrderId",
    "Method",
    "Status",
    "PaidAt",
    "TransactionId"
FROM "Payments"
WHERE "Method" = 'PayPal'
ORDER BY "Id" DESC
LIMIT 10;

-- 2. Count payment statuses
SELECT 
    "Status",
    COUNT(*) as total,
    COUNT("TransactionId") as with_txn_id
FROM "Payments"
WHERE "Method" = 'PayPal'
GROUP BY "Status";

-- 3. Find incomplete payments (needs attention)
SELECT 
    "Id",
    "OrderId",
    "Status",
    "PaidAt",
    "TransactionId"
FROM "Payments"
WHERE "Method" = 'PayPal'
AND "Status" = 'Pending'
AND "TransactionId" IS NULL
ORDER BY "Id" DESC;
```

### **Payment Success Rate:**
```sql
SELECT 
    DATE("PaidAt") as date,
    COUNT(*) as total,
    COUNT(CASE WHEN "Status" = 'Paid' THEN 1 END) as paid,
    COUNT(CASE WHEN "Status" = 'Pending' THEN 1 END) as pending,
    ROUND(
        100.0 * COUNT(CASE WHEN "Status" = 'Paid' THEN 1 END) / COUNT(*),
        2
    ) as success_rate
FROM "Payments"
WHERE "Method" = 'PayPal'
AND "PaidAt" >= CURRENT_DATE - INTERVAL '7 days'
GROUP BY DATE("PaidAt")
ORDER BY date DESC;
```

---

## ?? **TROUBLESHOOTING**

### **Issue 1: Column Already Exists Error**
```
Error: column "TransactionId" of relation "Payments" already exists

Solution:
? Column already added - skip this step
? Continue with application deployment
```

### **Issue 2: TransactionId Still NULL After Payment**
```
Symptoms:
- Payment approved on PayPal
- Status updates to "Paid"
- But TransactionId is NULL

Checks:
1. Check application logs for "Payment updated for order X"
2. Verify capture.id is not null in logs
3. Check UpdateStatusAsync is receiving transactionId parameter

Solution:
-- Check if capture is successful
-- Look in logs for: "PayPal payment captured successfully"
-- TransactionId should be in the log message
```

### **Issue 3: Build Errors**
```
Error: Cannot find CreatePayPalOrderRequest

Solution:
? Ensure using statement exists:
   using ShoesEcommerce.Models.ViewModels;

? Rebuild solution:
   dotnet clean
   dotnet build
```

### **Issue 4: Migration Already Applied**
```
Error: Migration 'AddTransactionIdToPayment' already applied

Solution:
? Check if column exists:
   SELECT * FROM information_schema.columns 
   WHERE table_name = 'Payments' 
   AND column_name = 'TransactionId';

? If exists, skip migration
? Continue with deployment
```

---

## ?? **MONITORING**

### **After Deployment, Monitor:**

#### **1. Application Logs:**
```
Watch for:
? "PayPal order created: PayPalOrderId=xxx"
? "Capturing PayPal order: PayPalOrderId=xxx"
? "Payment updated for order X: Status=Paid, TransactionId=xxx"

Errors to watch:
? "Payment not found for order X"
? "No capture information found"
? "Error updating payment status"
```

#### **2. Database Metrics:**
```sql
-- Payment completion rate (last 24 hours)
SELECT 
    COUNT(*) as total_payments,
    COUNT(CASE WHEN "Status" = 'Paid' THEN 1 END) as paid,
    COUNT(CASE WHEN "Status" = 'Pending' THEN 1 END) as pending,
    COUNT("TransactionId") as with_transaction_id
FROM "Payments"
WHERE "Method" = 'PayPal'
AND "PaidAt" >= NOW() - INTERVAL '24 hours';
```

#### **3. PayPal Dashboard:**
```
Compare:
- Payments in your Supabase ? Payments table
- Transactions in PayPal Dashboard

Should match:
? Number of transactions
? Transaction IDs
? Amounts
```

---

## ?? **ROLLBACK PROCEDURE**

### **If Something Goes Wrong:**

#### **Step 1: Rollback Database**
```sql
-- Remove TransactionId column
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "TransactionId";

-- Drop index
DROP INDEX IF EXISTS idx_payments_transactionid;

-- Restore from backup (if needed)
DROP TABLE "Payments";
ALTER TABLE "Payments_Backup_20251211" RENAME TO "Payments";
```

#### **Step 2: Rollback Code**
```bash
# Revert to previous version
git log --oneline -10
git revert <commit-hash>

# Or restore files:
# - PaymentRepository.cs
# - Payment.cs
# - PaymentController.cs
# Delete: PaymentViewModels.cs
```

#### **Step 3: Rebuild & Redeploy**
```bash
dotnet clean
dotnet build
dotnet run
```

---

## ? **POST-DEPLOYMENT CHECKLIST**

### **Immediate (Day 1):**
```
? Application starts successfully
? No database connection errors
? Test PayPal payment works
? TransactionId saves correctly
? Status updates to "Paid"
? PaidAt has timestamp
? Logs show successful updates
```

### **Short-term (Week 1):**
```
? Monitor payment success rate
? Check for any NULL TransactionIds
? Verify PayPal reconciliation
? Review error logs
? Test VNPay payments (also use TransactionId)
? Test COD orders (should still work)
```

### **Long-term (Month 1):**
```
? Payment completion rate stable/improved
? No data inconsistencies
? Admin can verify payments easily
? Dispute resolution easier
? Audit trail complete
```

---

## ?? **SUCCESS METRICS**

### **Before Fix:**
```
? TransactionId: N/A
? Payment Tracking: Incomplete
? Status Updates: Sometimes fail
? Admin Verification: Difficult
? Dispute Resolution: Hard
```

### **After Fix:**
```
? TransactionId: Captured
? Payment Tracking: Complete
? Status Updates: Reliable
? Admin Verification: Easy
? Dispute Resolution: Simple
```

---

## ?? **SUPPORT**

### **Common Issues:**

#### **Database Connection:**
```
Check: appsettings.json ? ConnectionStrings
Verify: Supabase project is running
Test: Can connect via Supabase dashboard
```

#### **PayPal Integration:**
```
Check: appsettings.json ? PayPal settings
Verify: ClientId and ClientSecret correct
Test: Can create test order
```

#### **Logging:**
```
Enable verbose logging:
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "ShoesEcommerce": "Debug"
  }
}
```

---

## ?? **FINAL CHECKS**

```
Run this checklist before considering deployment complete:

1. ? Database migration applied successfully
2. ? Application builds without errors
3. ? Application starts without errors
4. ? Test PayPal payment completes
5. ? TransactionId saves to database
6. ? Status updates to "Paid"
7. ? PaidAt has correct timestamp
8. ? Logs show successful payment capture
9. ? Admin can see payment details
10. ? No errors in application logs
```

---

**Deployment Status:** Ready to deploy ?  
**Estimated Time:** 15-30 minutes  
**Risk Level:** Low (backward compatible)  
**Rollback Time:** < 5 minutes  

?? **You're ready to deploy the PayPal payment sync fix!** ?
