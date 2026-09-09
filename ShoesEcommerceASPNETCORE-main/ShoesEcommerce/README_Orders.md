# Hệ thống Quản lý Đơn hàng - ShoesEcommerce

## Tổng quan
Hệ thống quản lý đơn hàng cho website bán giày ShoesEcommerce, được xây dựng bằng ASP.NET Core MVC với Entity Framework Core.

## Tính năng chính

### 1. Quản lý đơn hàng
- **Xem danh sách đơn hàng**: Khách hàng có thể xem tất cả đơn hàng của mình
- **Chi tiết đơn hàng**: Hiển thị đầy đủ thông tin về sản phẩm, địa chỉ giao hàng, thanh toán
- **Trạng thái đơn hàng**: Theo dõi trạng thái xử lý và thanh toán

### 2. Quy trình thanh toán
- **Checkout**: Trang thanh toán với giao diện thân thiện
- **Chọn địa chỉ giao hàng**: Từ danh sách địa chỉ có sẵn hoặc thêm mới
- **Phương thức thanh toán**: COD, Chuyển khoản ngân hàng, VNPay
- **Xác nhận đơn hàng**: Tạo đơn hàng và xóa giỏ hàng

### 3. Quản lý địa chỉ giao hàng
- **Thêm địa chỉ mới**: Modal form để thêm địa chỉ giao hàng
- **Chọn địa chỉ**: Radio button để chọn địa chỉ giao hàng
- **Lưu trữ địa chỉ**: Lưu vào database để sử dụng lại

## Cấu trúc dự án

### Models
```
Models/
├── Orders/
│   ├── Order.cs              # Model đơn hàng chính
│   ├── OrderDetail.cs        # Chi tiết sản phẩm trong đơn hàng
│   ├── Payment.cs            # Thông tin thanh toán
│   ├── ShippingAddress.cs    # Địa chỉ giao hàng
│   └── Invoice.cs            # Hóa đơn
└── ViewModels/
    └── OrderViewModels.cs    # ViewModels cho Orders
```

### Services
```
Services/
├── IOrderService.cs          # Interface cho OrderService
└── OrderService.cs           # Implementation của OrderService
```

### Controllers
```
Controllers/
└── OrderController.cs        # Controller xử lý các action liên quan đến Orders
```

### Views
```
Views/Order/
├── Index.cshtml             # Danh sách đơn hàng
├── Details.cshtml           # Chi tiết đơn hàng
├── Checkout.cshtml          # Trang thanh toán
└── Success.cshtml           # Trang thành công
```

## Cách sử dụng

### 1. Xem danh sách đơn hàng
```
GET /Order
```
- Hiển thị tất cả đơn hàng của khách hàng đã đăng nhập
- Sắp xếp theo thứ tự mới nhất
- Hiển thị trạng thái và tổng tiền

### 2. Xem chi tiết đơn hàng
```
GET /Order/Details/{id}
```
- Hiển thị đầy đủ thông tin đơn hàng
- Danh sách sản phẩm đã đặt
- Thông tin giao hàng và thanh toán
- Có thể cập nhật trạng thái thanh toán

### 3. Thanh toán đơn hàng
```
GET /Order/Checkout
POST /Order/Checkout
```
- Hiển thị sản phẩm trong giỏ hàng
- Chọn địa chỉ giao hàng
- Chọn phương thức thanh toán
- Xác nhận và tạo đơn hàng

### 4. Thêm địa chỉ giao hàng
```
POST /Order/CreateShippingAddress
```
- AJAX call để thêm địa chỉ mới
- Tự động cập nhật danh sách địa chỉ
- Validation form phía client và server

### 5. Cập nhật trạng thái thanh toán
```
POST /Order/UpdatePaymentStatus
```
- Cập nhật trạng thái thanh toán
- AJAX call để cập nhật real-time

## Cấu hình Database

### Connection String
Thêm connection string vào `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_server;Database=ShoesEcommerce;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### Migration
Tạo migration để cập nhật database:
```bash
dotnet ef migrations add AddOrders
dotnet ef database update
```

## Dependencies

### NuGet Packages
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.AspNetCore.Mvc`

### Services Registration
Trong `Program.cs`:
```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
```

## Bảo mật

### Authentication
- Tất cả các action đều yêu cầu đăng nhập (`[Authorize]`)
- Kiểm tra quyền truy cập đơn hàng theo CustomerId

### Validation
- Validation phía client và server
- Anti-forgery token cho tất cả form
- Kiểm tra dữ liệu đầu vào

## Giao diện

### Responsive Design
- Sử dụng Bootstrap 5
- Giao diện thân thiện với mobile
- Cards layout cho dễ đọc

### Icons
- Sử dụng Font Awesome
- Icons trực quan cho từng chức năng

### Color Scheme
- Primary: Xanh dương (#007bff)
- Success: Xanh lá (#28a745)
- Warning: Vàng (#ffc107)
- Info: Xanh nhạt (#17a2b8)
- Secondary: Xám (#6c757d)

## Xử lý lỗi

### Exception Handling
- Try-catch blocks trong tất cả action
- Log lỗi (cần implement logging service)
- Hiển thị thông báo lỗi thân thiện

### Validation Errors
- ModelState validation
- Hiển thị lỗi validation trong form
- Client-side validation với jQuery

## Tương lai

### Tính năng có thể mở rộng
- **Email confirmation**: Gửi email xác nhận đơn hàng
- **SMS notification**: Thông báo qua SMS
- **Order tracking**: Theo dõi vận chuyển
- **Return/Refund**: Xử lý đổi trả, hoàn tiền
- **Loyalty program**: Chương trình khách hàng thân thiết
- **Analytics**: Thống kê đơn hàng, doanh thu

### Cải tiến kỹ thuật
- **Caching**: Redis cache cho performance
- **Background jobs**: Xử lý đơn hàng bất đồng bộ
- **Microservices**: Tách thành các service riêng biệt
- **API**: RESTful API cho mobile app

## Hỗ trợ

Nếu có vấn đề gì, vui lòng liên hệ:
- Email: support@shoesecommerce.com
- Hotline: 1900-xxxx
- Giờ làm việc: 8:00 - 22:00
