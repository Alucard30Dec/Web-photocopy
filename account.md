# WebPhotocopy / PhotoCopyHub - Tài khoản test

> File này dùng cho môi trường local/dev để đăng nhập kiểm thử.
> Không dùng các tài khoản/mật khẩu này cho production.
> Database dev đang dùng PostgreSQL/Supabase qua Session Pooler trong user-secrets/env; file này không lưu connection string database.

## Trạng thái seed

- `SeedSampleData:Enabled=true`
- Seed chạy bằng `PHOTOCOPYHUB_SEED_ONLY=true`
- Dữ liệu mẫu dùng prefix/key `seed-*` để có thể chạy lại mà không nhân đôi dữ liệu mẫu.
- Slug cơ sở test chính: `ToanPhotocopy`

## Đường dẫn chính

| Khu vực | URL |
|---|---|
| Trang chủ | https://localhost:7250/Home |
| Danh sách cơ sở | https://localhost:7250/Shops |
| Cơ sở Toàn Photocopy | https://localhost:7250/ToanPhotocopy |
| Đăng nhập khách hàng | https://localhost:7250/ToanPhotocopy/Login |
| Dashboard khách hàng | https://localhost:7250/ToanPhotocopy/Dashboard |
| Đăng nhập shop/admin cơ sở | https://localhost:7250/ToanPhotocopy/Admin/Login |
| Dashboard shop/admin cơ sở | https://localhost:7250/ToanPhotocopy/Admin |
| Đăng nhập admin hệ thống | https://localhost:7250/Admin/Login |
| Dashboard admin hệ thống | https://localhost:7250/Admin |
| Swagger API | https://localhost:7250/swagger |
| Health DB | https://localhost:7250/healthz/db |

## Tài khoản test

| Vai trò | Email | Mật khẩu | URL đăng nhập | Dữ liệu mẫu chính |
|---|---|---|---|---|
| Admin hệ thống | admin@photocopyhub.local | Admin@123456 | https://localhost:7250/Admin/Login | Quản trị hệ thống, duyệt nạp ví lớn, quản lý người dùng, đối soát |
| Nhân viên tiệm / ShopOperator | operator@photocopyhub.local | Operator@123456 | https://localhost:7250/ToanPhotocopy/Admin/Login | Nhận đơn, xác nhận file, cập nhật trạng thái, nạp tiền tại quầy |
| Khách hàng mặc định | khachhang@photocopyhub.local | Customer@123 | https://localhost:7250/ToanPhotocopy/Login | Có yêu cầu nạp ví bị từ chối và đơn in mới gửi |
| Khách hàng 01 | sinhvien01@photocopyhub.local | Student@123 | https://localhost:7250/ToanPhotocopy/Login | Có ví đã nạp, đơn in hoàn tất, đơn sản phẩm, đơn dịch vụ scan |
| Khách hàng 02 | sinhvien02@photocopyhub.local | Student@123 | https://localhost:7250/ToanPhotocopy/Login | Có ví đã nạp, yêu cầu nạp đang chờ duyệt, đơn in đang xử lý, đơn đóng gáy |
| Khách hàng 03 | sinhvien03@photocopyhub.local | Student@123 | https://localhost:7250/ToanPhotocopy/Login | Có yêu cầu nạp lớn chờ admin duyệt bước 2, đơn in đã xác nhận, đơn sản phẩm hoàn tất |

## Dữ liệu mẫu đã seed

| Nhóm dữ liệu | Nội dung |
|---|---|
| Bảng giá in | 14 rule cho A5/A4/A3/A0, trắng đen/màu, một mặt/hai mặt, ảnh màu A4 |
| Sản phẩm | 8 sản phẩm văn phòng phẩm, giấy, bìa, kẹp, sổ tay, mực dấu |
| Dịch vụ hỗ trợ | 7 dịch vụ: đóng gáy, ép plastic, scan, đánh máy, bấm kim, cán màng, chỉnh file |
| Ví/nạp tiền | Đủ trạng thái: đã duyệt, chờ duyệt, từ chối, chờ admin duyệt bước 2 |
| Đơn in | Đủ trạng thái test chính: Submitted, ConfirmedByShop, Processing, Completed |
| Đơn sản phẩm | Có đơn đang xử lý và hoàn tất, kèm stock movements |
| Đơn dịch vụ | Có đơn đã hoàn tất và đơn mới gửi |
| File upload | PDF mẫu trong `App_Data/uploads/seed` |

## Flow test nhanh

### Khách hàng

1. Mở `https://localhost:7250/ToanPhotocopy/Login`.
2. Đăng nhập bằng một trong các tài khoản khách hàng ở trên.
3. Vào `https://localhost:7250/ToanPhotocopy/Dashboard`.
4. Test các chức năng: tạo đơn in, xem đơn của tôi, nạp ví, mua sản phẩm, đặt dịch vụ hỗ trợ, cập nhật hồ sơ.

### Tiệm photocopy

1. Mở `https://localhost:7250/ToanPhotocopy/Admin/Login`.
2. Đăng nhập bằng `operator@photocopyhub.local` / `Operator@123456`.
3. Test các chức năng: nhận đơn, kiểm tra file, xác nhận thanh toán, cập nhật trạng thái đơn, xử lý nạp ví, tồn kho.

### Admin hệ thống

1. Mở `https://localhost:7250/Admin/Login`.
2. Đăng nhập bằng `admin@photocopyhub.local` / `Admin@123456`.
3. Test quản trị người dùng, ví, nạp tiền lớn cần duyệt bước 2, báo cáo và đối soát.

## Ghi chú xử lý lỗi

- Nếu đăng nhập báo sai mật khẩu, chạy lại seed-only để DbInitializer reset password test.
- Nếu app không khởi động được do database, kiểm tra user-secrets đang dùng Session Pooler, không dùng Direct host `db.<project-ref>.supabase.co`.
- Nếu ví lệch, chạy lại seed-only; các record `seed-*` sẽ được tái tạo và `CurrentBalance` của tài khoản test sẽ được tính lại theo ledger.
