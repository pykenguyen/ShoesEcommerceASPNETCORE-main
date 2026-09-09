-- ============================================================================
-- SUPABASE SQL MIGRATION: Add TransactionId to Payments Table
-- ============================================================================
-- Purpose: Store PayPal/VNPay transaction IDs for payment tracking
-- Date: 2025-12-11
-- Author: System
-- ============================================================================

-- Step 1: Check if column already exists
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_name = 'Payments' 
        AND column_name = 'TransactionId'
    ) THEN
        -- Step 2: Add TransactionId column
        ALTER TABLE "Payments" 
        ADD COLUMN "TransactionId" TEXT;
        
        RAISE NOTICE 'Column TransactionId added successfully';
    ELSE
        RAISE NOTICE 'Column TransactionId already exists';
    END IF;
END $$;

-- Step 3: Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_payments_transactionid 
ON "Payments" ("TransactionId") 
WHERE "TransactionId" IS NOT NULL;

-- Step 4: Verify the change
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns
WHERE table_name = 'Payments'
AND column_name = 'TransactionId';

-- Step 5: View current Payments table structure
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'Payments'
ORDER BY ordinal_position;

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================

-- Check all payments (should now have TransactionId column)
SELECT 
    "Id",
    "OrderId",
    "Method",
    "Status",
    "PaidAt",
    "TransactionId"
FROM "Payments"
ORDER BY "Id" DESC
LIMIT 10;

-- Count payments by status
SELECT 
    "Status",
    COUNT(*) as count,
    COUNT("TransactionId") as with_transaction_id,
    COUNT(*) - COUNT("TransactionId") as without_transaction_id
FROM "Payments"
GROUP BY "Status"
ORDER BY count DESC;

-- Find PayPal payments without transaction ID (needs attention)
SELECT 
    "Id",
    "OrderId",
    "Method",
    "Status",
    "PaidAt",
    "TransactionId"
FROM "Payments"
WHERE "Method" = 'PayPal' 
AND "Status" = 'Pending'
AND "TransactionId" IS NULL
ORDER BY "Id" DESC;

-- ============================================================================
-- OPTIONAL: UPDATE EXISTING DATA (if you have transaction IDs)
-- ============================================================================

-- Example: Update a specific payment with transaction ID
-- UNCOMMENT AND MODIFY AS NEEDED:

/*
UPDATE "Payments"
SET 
    "TransactionId" = 'PAYID-XXXXXX',
    "Status" = 'Paid',
    "PaidAt" = NOW()
WHERE "OrderId" = 18;
*/

-- Example: Bulk update for testing (DO NOT USE IN PRODUCTION)
/*
UPDATE "Payments"
SET "TransactionId" = 'TEST-' || "Id"::TEXT
WHERE "Method" = 'PayPal' 
AND "TransactionId" IS NULL
AND "Status" = 'Pending';
*/

-- ============================================================================
-- ROLLBACK SCRIPT (if needed)
-- ============================================================================

-- UNCOMMENT TO REMOVE THE COLUMN (USE WITH CAUTION!):
/*
-- Drop index first
DROP INDEX IF EXISTS idx_payments_transactionid;

-- Drop column
ALTER TABLE "Payments" DROP COLUMN IF EXISTS "TransactionId";

-- Verify removal
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'Payments';
*/

-- ============================================================================
-- MONITORING QUERIES
-- ============================================================================

-- Monitor PayPal payment success rate
SELECT 
    DATE("PaidAt") as payment_date,
    COUNT(*) as total_paypal_payments,
    COUNT(CASE WHEN "Status" = 'Paid' THEN 1 END) as successful,
    COUNT(CASE WHEN "Status" = 'Pending' THEN 1 END) as pending,
    COUNT(CASE WHEN "Status" = 'Failed' THEN 1 END) as failed,
    COUNT("TransactionId") as with_transaction_id
FROM "Payments"
WHERE "Method" = 'PayPal'
AND "PaidAt" IS NOT NULL
GROUP BY DATE("PaidAt")
ORDER BY payment_date DESC;

-- Check recent payment activity
SELECT 
    p."Id",
    p."OrderId",
    p."Method",
    p."Status",
    p."PaidAt",
    p."TransactionId",
    o."TotalAmount",
    c."Email" as customer_email
FROM "Payments" p
JOIN "Orders" o ON p."OrderId" = o."Id"
JOIN "Customers" c ON o."CustomerId" = c."Id"
ORDER BY p."Id" DESC
LIMIT 20;

-- ============================================================================
-- PERFORMANCE CHECK
-- ============================================================================

-- Check table size
SELECT 
    pg_size_pretty(pg_total_relation_size('"Payments"')) as total_size,
    pg_size_pretty(pg_relation_size('"Payments"')) as table_size,
    pg_size_pretty(pg_indexes_size('"Payments"')) as indexes_size;

-- Check index usage
SELECT 
    schemaname,
    tablename,
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes
WHERE tablename = 'Payments';

-- ============================================================================
-- END OF MIGRATION SCRIPT
-- ============================================================================

COMMIT;
