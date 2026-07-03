# Tài Khoản Test WebPhotocopyHub

File này dùng cho database local `WebPhotocopyHub`.

Không dùng các tài khoản/mật khẩu này cho production.

## Tóm Tắt Nhanh

| Nhóm | Email đăng nhập | Mật khẩu | Role hệ thống | Ghi chú |
|---|---|---|---|---|
| Admin | `admin@photocopyhub.local` | `Admin@123456` | `Admin` | Quản trị toàn hệ thống |
| Nhân viên shop | `operator@photocopyhub.local` | `Operator@123456` | `ShopOperator` | Quản lý cả 3 chi nhánh |
| Khách hàng | `sinhvien01@webphotocopyhub.local` | `Student@123` | `Customer` | Khách test đặt in |
| Khách hàng | `sinhvien02@webphotocopyhub.local` | `Student@123` | `Customer` | Khách test đặt in |
| Khách hàng | `sinhvien03@webphotocopyhub.local` | `Student@123` | `Customer` | Khách test đặt in |
| Khách hàng | `khachhang@webphotocopyhub.local` | `Customer@123` | `Customer` | Khách hàng mặc định |

## Shop Và Role Đang Gán

| Mã shop | Slug | Tên shop | Tài khoản | Role tại shop | Chính |
|---|---|---|---|---|---|
| `TOAN` | `toanphotocopy` | Toàn Photocopy | `operator@photocopyhub.local` | Quản lý cơ sở | Có |
| `DBP141` | `141-dien-bien-phu` | WebPhotocopyHub 141 Điện Biên Phủ | `operator@photocopyhub.local` | Quản lý cơ sở | Không |
| `CENTER` | `co-so-trung-tam` | WebPhotocopyHub Cơ sở trung tâm | `operator@photocopyhub.local` | Quản lý cơ sở | Không |

Tất cả shop hiện đang bật các tính năng:

`PRINT_ORDERS`, `PRODUCT_SALES`, `SUPPORT_SERVICES`, `TOP_UPS`, `WALLET`, `INVENTORY`, `PRICING`, `REPORTS`.

## Role Hệ Thống

| Role | Mục đích | Tài khoản hiện có |
|---|---|---|
| `Admin` | Quản trị user, role, phân quyền, danh mục, cấu hình hệ thống, báo cáo tổng | `admin@photocopyhub.local` |
| `ShopOperator` | Vận hành shop, xử lý đơn, duyệt nạp tiền, quản lý kho/dịch vụ theo chi nhánh được gán | `operator@photocopyhub.local` |
| `Customer` | Đặt in, nạp ví, đặt sản phẩm/dịch vụ hỗ trợ | 4 tài khoản khách hàng |

## Role Tại Mỗi Shop

Các role dưới đây tồn tại tại cả 3 shop `TOAN`, `DBP141`, `CENTER`.

| Role shop | Quyền chính |
|---|---|
| Quản lý cơ sở | Xem dashboard, xử lý đơn in, xem/tải file in, hoàn tiền đơn in, xử lý đơn sản phẩm, xử lý đơn dịch vụ hỗ trợ, nạp tiền tại quầy, duyệt nạp tiền, xem báo cáo, xem/điều chỉnh tồn kho |
| Nhân viên in ấn | Xem dashboard, xem đơn in, xử lý đơn in, xem/tải file in |
| Nhân viên kho | Xem dashboard, xem/điều chỉnh tồn kho, xem/xử lý đơn sản phẩm |
| Thu ngân | Xem dashboard, xem/duyệt nạp tiền, nạp tiền tại quầy |
| Chỉ xem báo cáo | Xem dashboard và báo cáo |

## Đường Dẫn Đăng Nhập Gợi Ý

Port local thường dùng: `https://localhost:7250`.

| Nhóm | Đường dẫn |
|---|---|
| Trang chủ | `https://localhost:7250/Home` |
| Danh sách shop | `https://localhost:7250/Shops` |
| Admin hệ thống | `https://localhost:7250/Admin/Login` |
| Shop TOAN | `https://localhost:7250/toanphotocopy` |
| Khách hàng shop TOAN | `https://localhost:7250/toanphotocopy/Login` |
| Shop operator TOAN | `https://localhost:7250/toanphotocopy/Admin/Login` |
| Shop DBP141 | `https://localhost:7250/141-dien-bien-phu` |
| Khách hàng shop DBP141 | `https://localhost:7250/141-dien-bien-phu/Login` |
| Shop operator DBP141 | `https://localhost:7250/141-dien-bien-phu/Admin/Login` |
| Shop CENTER | `https://localhost:7250/co-so-trung-tam` |
| Khách hàng shop CENTER | `https://localhost:7250/co-so-trung-tam/Login` |
| Shop operator CENTER | `https://localhost:7250/co-so-trung-tam/Admin/Login` |
| Swagger API | `https://localhost:7250/swagger` |
| Health DB | `https://localhost:7250/healthz/db` |

## Dữ Liệu Mẫu Đang Có

| Nhóm dữ liệu | Nội dung |
|---|---|
| Chi nhánh | 3 shop: `TOAN`, `DBP141`, `CENTER` |
| User | 1 admin, 1 shop operator, 4 customer |
| Role shop | 5 role cho mỗi shop: Quản lý cơ sở, Nhân viên in ấn, Nhân viên kho, Thu ngân, Chỉ xem báo cáo |
| Bảng giá in | Có rule theo khổ giấy, kiểu in, màu in, ảnh/tài liệu |
| Sản phẩm | Có sản phẩm văn phòng phẩm/giấy/bìa/kẹp... theo chi nhánh |
| Dịch vụ hỗ trợ | Có dịch vụ scan, đóng gáy, ép plastic, chỉnh file... |
| Ví/nạp tiền | Có ví theo chi nhánh, giao dịch ví và yêu cầu nạp tiền |
| Đơn in | Có đơn in ở nhiều trạng thái để test xử lý |
| Đơn sản phẩm | Có đơn sản phẩm và lịch sử nhập/xuất/tồn kho |
| Đơn dịch vụ | Có đơn dịch vụ hỗ trợ |

## Flow Test Nhanh

### Admin hệ thống

1. Mở `https://localhost:7250/Admin/Login`.
2. Đăng nhập `admin@photocopyhub.local` / `Admin@123456`.
3. Test quản trị người dùng, role, phân quyền, danh mục, báo cáo và đối soát.

### Nhân viên shop

1. Mở trang login theo shop, ví dụ `https://localhost:7250/toanphotocopy/Admin/Login`.
2. Đăng nhập `operator@photocopyhub.local` / `Operator@123456`.
3. Có thể dùng cùng tài khoản này cho `TOAN`, `DBP141`, hoặc `CENTER`.
4. Test nhận đơn, kiểm tra file, cập nhật trạng thái đơn, duyệt nạp ví, quản lý tồn kho.

### Khách hàng

1. Mở trang đăng nhập theo slug shop, ví dụ `https://localhost:7250/toanphotocopy/Login`.
2. Đăng nhập bằng một tài khoản customer trong bảng tóm tắt.
3. Test tạo đơn in, nạp ví, mua sản phẩm, đặt dịch vụ hỗ trợ, xem lịch sử đơn.

## Kiểm Tra Trong pgAdmin

| Cần xem | Vị trí |
|---|---|
| Bảng TKS canonical | `WebPhotocopyHub` -> `Schemas` -> `public` -> `Tables` |
| View TKS canonical | `WebPhotocopyHub` -> `Schemas` -> `public` -> `Views` |
| Store/routine TKS | `WebPhotocopyHub` -> `Schemas` -> `public` -> `Functions` |
| Bảng app cũ được clone | `WebPhotocopyHub` -> `Schemas` -> `app` -> `Tables` |
| Bảng quyền hệ thống cũ | `WebPhotocopyHub` -> `Schemas` -> `system` -> `Tables` |
| Audit cũ | `WebPhotocopyHub` -> `Schemas` -> `audit` -> `Tables` |

## Lưu Ý

- Email admin/operator đúng theo database hiện tại là `@photocopyhub.local`, không phải `@webphotocopyhub.local`.
- Các mật khẩu trên là mật khẩu seed/test local, không dùng cho môi trường thật.
- Nếu app được seed lại bằng cấu hình khác trong `appsettings` hoặc user-secrets, cần cập nhật lại file này theo database mới.
- Nếu đăng nhập báo sai mật khẩu, kiểm tra app đang trỏ đúng database `WebPhotocopyHub` và không dùng cache/password cũ.
