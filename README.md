# PhotoCopyHub

Hệ thống web quản lý tiệm photocopy/in ấn theo mô hình **Monolith ASP.NET Core MVC**.

## 1. Công nghệ

- .NET 8
- ASP.NET Core MVC + Razor Views + Bootstrap 5
- ASP.NET Core Identity (xác thực + phân quyền)
- Entity Framework Core Code First
- SQLite mặc định (chạy local nhanh)
- Có thể chuyển sang SQL Server bằng connection string

## 2. Kiến trúc solution

```text
PhotoCopyHub.sln
├─ src/
│  ├─ PhotoCopyHub.Domain
│  ├─ PhotoCopyHub.Application
│  ├─ PhotoCopyHub.Infrastructure
│  └─ PhotoCopyHub.Web
└─ tests/
   └─ PhotoCopyHub.Tests
```

## 3. Chức năng chính

### Customer

- Đăng ký/đăng nhập
- Xem và cập nhật hồ sơ cá nhân
- Xem số dư ví + lịch sử giao dịch
- Tạo yêu cầu nạp tiền chuyển khoản (có thể upload ảnh minh chứng)
- Xem lịch sử yêu cầu nạp tiền
- Tạo đơn in (upload file, chọn cấu hình in, hệ thống tự tính tiền và trừ ví)
- Xem danh sách file đã upload
- Đặt mua văn phòng phẩm
- Đặt dịch vụ hỗ trợ

### Admin

- Dashboard tổng quan
- Quản lý users + điều chỉnh số dư bằng giao dịch có log
- Xem toàn bộ lịch sử giao dịch ví
- Duyệt/từ chối yêu cầu nạp tiền
- Quản lý đơn in + tải file khách upload + cập nhật trạng thái + hoàn tiền
- Quản lý sản phẩm văn phòng phẩm
- Quản lý dịch vụ hỗ trợ
- Quản lý bảng giá in
- Xem audit logs

### ShopOperator

- Dashboard vận hành và hàng chờ xử lý
- Duyệt bước 1 yêu cầu nạp tiền, nạp tiền tại quầy
- Xử lý đơn in, cập nhật trạng thái, tải file in
- Quản lý tồn kho văn phòng phẩm và trạng thái dịch vụ hỗ trợ

## 4. Cấu hình môi trường

### Yêu cầu

- Cài **.NET SDK 8 hoặc 9** (project target `net8.0`, `global.json` đang ghim `9.0.308`)

Kiểm tra:

```bash
dotnet --version
```

### Connection string mặc định (SQLite)

Trong `src/PhotoCopyHub.Web/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=photocopyhub.db"
}
```

### Đổi sang SQL Server (khi cần)

1. Đổi `"DatabaseProvider"` thành `"SqlServer"` trong `src/PhotoCopyHub.Web/appsettings.json`.
2. Sửa `ConnectionStrings:DefaultConnection` sang SQL Server.
3. Tạo migration mới phù hợp DB mới (nếu cần khác biệt schema/provider).

## 5. Tài khoản admin mặc định (Development)

Khi chạy ở `ASPNETCORE_ENVIRONMENT=Development`, hệ thống tự seed:

- Email: `admin@photocopyhub.local`
- Password: `Admin@123456`

Có thể đổi tại:

```json
"SeedAdmin": {
  "Email": "admin@photocopyhub.local",
  "Password": "Admin@123456",
  "FullName": "Quản trị hệ thống"
}
```

Tài khoản ShopOperator mặc định:

- Email: `operator@photocopyhub.local`
- Password: `Operator@123456`

## 6. Cách chạy project

### Chạy nhanh bằng CLI

```bash
dotnet restore
dotnet build PhotoCopyHub.sln
dotnet watch run --project src/PhotoCopyHub.Web/PhotoCopyHub.Web.csproj
```

Lưu ý: cookie xác thực cấu hình `Secure`, nên hãy truy cập bằng `https://localhost:7250`.

Ứng dụng sẽ tự:

- Apply migration trong môi trường Development
- Seed role + admin + dữ liệu mẫu (pricing/products/support services)
- Seed ShopOperator mặc định

### Chạy bằng VS Code

- `Ctrl+Shift+B`: chạy task mặc định `Run PhotoCopyHub (watch)`
- Debug: chọn cấu hình `Debug PhotoCopyHub.Web`

File cấu hình:

- `.vscode/tasks.json`
- `.vscode/launch.json`

## 7. Migration

Migration đầu tiên nằm tại:

- `src/PhotoCopyHub.Infrastructure/Data/Migrations/202603170001_InitialCreate.cs`
- `src/PhotoCopyHub.Infrastructure/Data/Migrations/202603240001_SecurityAndOperationsHardening.cs`

Nếu cần tạo migration mới:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/PhotoCopyHub.Infrastructure/PhotoCopyHub.Infrastructure.csproj \
  --startup-project src/PhotoCopyHub.Web/PhotoCopyHub.Web.csproj \
  --output-dir Data/Migrations
```

## 8. Unit tests

Đã có test cơ bản cho:

- Pricing service
- Wallet debit/credit logic
- Top-up approval flow
- Wallet idempotency
- Top-up multi-step approval + four-eyes
- Print job state machine + refund policy

Chạy test:

```bash
dotnet test tests/PhotoCopyHub.Tests/PhotoCopyHub.Tests.csproj
```

## 9. Lưu ý bảo mật MVP

- Role-based authorization cho toàn bộ module
- Policy-based authorization cho nghiệp vụ back-office
- Auto anti-forgery cho toàn bộ form POST
- Rate limiting cho login và luồng tài chính nhạy cảm
- Security headers (CSP, X-Frame-Options, X-Content-Type-Options...)
- Lưu file bằng tên GUID, không dùng tên gốc để lưu trực tiếp
- File upload lưu ngoài `wwwroot`, không public trực tiếp
- Kiểm tra extension + MIME + giới hạn dung lượng + magic number
- Mọi biến động số dư được ghi vào `WalletTransactions`
- Có idempotency key cho các thao tác tài chính nhạy cảm
- Có audit log cho thao tác quan trọng + hash chain chống chỉnh sửa ngầm
- Có màn hình đối soát số dư ví (CurrentBalance vs ledger)

## 10. Troubleshooting nhanh

- Nếu bị lỗi SDK không tương thích:
  - Kiểm tra `global.json` và `dotnet --list-sdks`.
  - Cài đúng SDK hoặc đổi version trong `global.json` cho khớp.
- Nếu chạy được web nhưng không đăng ký được do DB cũ lệch schema:
  - Dừng app.
  - Xóa các file:
    - `src/PhotoCopyHub.Web/photocopyhub.db`
    - `src/PhotoCopyHub.Web/photocopyhub.db-wal`
    - `src/PhotoCopyHub.Web/photocopyhub.db-shm`
  - Chạy lại `dotnet watch run ...`, app sẽ tự migrate + seed lại.

## 11. Deploy demo URL cố định miễn phí (Render + TiDB Cloud)

Mục tiêu: có URL dạng `https://<ten-app>.onrender.com` để gửi cho người khác dùng demo lâu dài.

### Bước 1: Chuẩn bị source

Trong repo đã có sẵn:

- `Dockerfile` để build/run app ASP.NET Core
- `render.yaml` để Render đọc cấu hình service
- Hỗ trợ chạy sau reverse proxy (HTTPS)

Push code lên GitHub/GitLab:

```bash
git add .
git commit -m "Add Render deployment setup"
git push
```

### Bước 2: Tạo database TiDB Cloud (MySQL-compatible)

1. Tạo tài khoản TiDB Cloud.
2. Tạo cluster Serverless/Starter.
3. Lấy connection string (format MySQL).
4. Vào phần network access:
   - Cho demo nhanh: cho phép `0.0.0.0/0` (kém an toàn hơn, chỉ nên dùng demo).
   - An toàn hơn: giới hạn theo IP outbound cố định (nếu có).

Ví dụ connection string:

```text
Server=<host>;Port=4000;Database=<db>;User Id=<user>;Password=<password>;SslMode=Required;AllowPublicKeyRetrieval=True;ConnectionTimeout=30;DefaultCommandTimeout=60;
```

### Bước 3: Tạo web service trên Render

1. Đăng nhập Render -> `New` -> `Web Service`.
2. Chọn repo của project.
3. Render sẽ nhận `render.yaml` tự động. Nếu cần, chọn:
   - Runtime: `Docker`
   - Plan: `Free`
4. Thêm/kiểm tra các biến môi trường:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `DatabaseProvider=MySql`
   - `Database__UseSqliteFallbackWhenPrimaryUnavailable=false`
   - `ConnectionStrings__DefaultConnection=<connection-string-tidb>`
   - (tuỳ chọn) `SeedSampleData__Enabled=true`
   - (khuyến nghị) đổi tài khoản seed:
     - `SeedAdmin__Email`, `SeedAdmin__Password`
     - `SeedShopOperator__Email`, `SeedShopOperator__Password`
5. Bấm `Create Web Service` và chờ deploy xong.

### Bước 4: Dùng URL cố định

- URL mặc định của Render là cố định theo tên service:
  - `https://<service-name>.onrender.com`
- Bạn có thể gửi URL này cho người khác dùng demo.

### Lưu ý vận hành bản free

- Free web service có thể sleep khi không có truy cập, request đầu tiên sẽ chậm.
- Lưu trữ file local trong container không bền vững (restart/redeploy có thể mất file upload).
- Không commit secrets thật vào repo; lưu ở Environment Variables của Render.
