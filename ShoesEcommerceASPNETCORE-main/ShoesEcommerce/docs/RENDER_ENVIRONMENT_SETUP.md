# ?? H??ng d?n c?u hình Environment Variables trên Render

## B??c 1: Truy c?p Render Dashboard

1. ??ng nh?p https://dashboard.render.com
2. Ch?n Web Service c?a b?n (ShoesEcommerce)
3. Vào tab **Environment** trong sidebar

---

## B??c 2: Thêm các Environment Variables

Click **Add Environment Variable** và thêm t?ng bi?n sau:

### ?? Supabase Storage (REST API)

| Key | Value | Mô t? |
|-----|-------|-------|
| `SUPABASE_PROJECT_URL` | `https://wrrlgzyxojhlgwpunpud.supabase.co` | URL d? án Supabase |
| `SUPABASE_SERVICE_ROLE_KEY` | `eyJhbGciOiJIUzI1NiIsInR...` | Service Role Key t? Supabase Dashboard |
| `SUPABASE_BUCKET_NAME` | `images` | Tên bucket |

#### Cách l?y Service Role Key t? Supabase:
1. ??ng nh?p https://supabase.com/dashboard
2. Ch?n project `wrrlgzyxojhlgwpunpud`
3. Vào **Settings** ? **API**
4. Copy **service_role** key (NOT anon key!)

> ?? **QUAN TR?NG**: 
> - Service Role Key có full quy?n truy c?p, ch? dùng ? server-side
> - KHÔNG bao gi? expose key này ? client-side
> - Key b?t ??u b?ng `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`

---

### ??? Database (PostgreSQL)

| Key | Value |
|-----|-------|
| `DATABASE_CONNECTION_STRING` | `Host=aws-1-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.wrrlgzyxojhlgwpunpud;Password=<your-db-password>;Pooling=true;` |

---

### ?? PayPal (Optional - n?u dùng PayPal)

| Key | Value |
|-----|-------|
| `PAYPAL_CLIENT_ID` | `<your-paypal-client-id>` |
| `PAYPAL_CLIENT_SECRET` | `<your-paypal-client-secret>` |
| `PAYPAL_MODE` | `Sandbox` ho?c `Live` |

---

### ?? Twilio SMS (Optional - n?u dùng OTP)

| Key | Value |
|-----|-------|
| `TWILIO_ACCOUNT_SID` | `<your-account-sid>` |
| `TWILIO_AUTH_TOKEN` | `<your-auth-token>` |
| `TWILIO_PHONE_NUMBER` | `+1xxxxxxxxxx` |

---

### ?? Google OAuth (Optional)

| Key | Value |
|-----|-------|
| `GOOGLE_CLIENT_ID` | `<your-google-client-id>` |
| `GOOGLE_CLIENT_SECRET` | `<your-google-client-secret>` |

---

## B??c 3: Áp d?ng thay ??i

1. Sau khi thêm t?t c? bi?n, click **Save Changes**
2. Render s? t? ??ng redeploy ?ng d?ng
3. Ki?m tra logs ?? xác nh?n c?u hình ?úng

---

## ?? Ki?m tra c?u hình

Sau khi deploy, ki?m tra logs ?? xác nh?n:

```
?? Supabase Storage Configuration:
   - Project URL: https://wrrlgzyxojhlgwpunpud.supabase.co
   - Bucket: images
   - Service Role Key: eyJhbGciOiJIUzI1NiI...
? Supabase Storage configured successfully!
```

N?u th?y `NOT SET`, ki?m tra l?i tên bi?n và giá tr?.

---

## ?? Checklist tr??c khi deploy

- [ ] `SUPABASE_PROJECT_URL` = `https://wrrlgzyxojhlgwpunpud.supabase.co`
- [ ] `SUPABASE_SERVICE_ROLE_KEY` ?ã set (l?y t? Settings ? API ? service_role)
- [ ] `SUPABASE_BUCKET_NAME` = `images`
- [ ] `DATABASE_CONNECTION_STRING` ?ã set
- [ ] Bucket `images` ?ã ???c t?o và set **Public** trong Supabase Storage

---

## ?? L?i th??ng g?p

### 1. Upload failed: 401 Unauthorized
**Nguyên nhân**: Sai Service Role Key
**Gi?i pháp**: 
- Ki?m tra key ?úng là **service_role** (không ph?i anon key)
- Copy l?i key t? Supabase Dashboard ? Settings ? API

### 2. Upload failed: 404 Not Found
**Nguyên nhân**: Bucket ch?a t?n t?i
**Gi?i pháp**: 
- Vào Supabase ? Storage ? New Bucket ? ??t tên `images`
- Tick **Public bucket**

### 3. Upload failed: 400 Bad Request
**Nguyên nhân**: File quá l?n ho?c ??nh d?ng không h? tr?
**Gi?i pháp**:
- Ki?m tra file size < 50MB
- ??nh d?ng cho phép: .jpg, .jpeg, .png, .gif, .webp

### 4. Service Role Key tr?ng
**Nguyên nhân**: App ch?y ? Production nh?ng config n?m trong `appsettings.Development.json`
**Gi?i pháp**: 
- Set environment variable `SUPABASE_SERVICE_ROLE_KEY` trên Render

---

## ?? Template appsettings.json cho Local Development

```json
{
  "SupabaseStorage": {
    "ProjectUrl": "https://wrrlgzyxojhlgwpunpud.supabase.co",
    "ServiceRoleKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.YOUR_SERVICE_ROLE_KEY_HERE",
    "BucketName": "images"
  }
}
```

> ?? KHÔNG commit file này lên Git n?u ch?a key th?t!
> Thêm vào `.gitignore`: `appsettings.Development.json`
