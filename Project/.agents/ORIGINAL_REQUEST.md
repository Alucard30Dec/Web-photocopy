# Original User Request

## Initial Request — 2026-07-03T22:31:42Z

Refactor all remaining customer-facing views in the ASP.NET Core MVC project (`WebPhotocopyHub.Web.Customer`) to use the newly created Tailwind CSS layout (`_BranchCustomerModernLayout.cshtml`) and match the modern aesthetic of the Dashboard.

Working directory: `e:\OneDrive - 0dpmr\WebPhotocopy\Project`
Integrity mode: demo

## Requirements

### R1. Áp dụng Layout Mới
Thay đổi thuộc tính Layout của tất cả các trang (.cshtml) dành cho khách hàng (ví dụ: Profile, PrintJobs, Products, Wallet, SupportOrders) sang `~/Views/Shared/_BranchCustomerModernLayout.cshtml`. Loại bỏ hoàn toàn sự phụ thuộc vào Bootstrap.

### R2. Thiết kế lại UI theo chuẩn Apple/Linear
Thiết kế lại hoàn toàn cấu trúc HTML bên trong các trang này. Sử dụng thẻ Card trắng (`bg-surface-container-lowest`), bóng đổ nhẹ (`shadow-sm`), font chữ hiện đại (Inter/Plus Jakarta Sans), và các nút bấm màu xanh dương (`bg-primary`) để đồng bộ tuyệt đối với phong cách của Dashboard. Các bảng dữ liệu (Tables) và Form nhập liệu cũng phải được Tailwind-hóa.

### R3. Giữ nguyên Logic Backend
Tuyệt đối giữ nguyên các liên kết Razor Model (`@model`, `asp-for`, `asp-action`, `asp-controller`) và logic hoạt động của các Form. Nhiệm vụ chỉ là "thay áo" giao diện ở tầng View, không làm thay đổi luồng xử lý dữ liệu.

## Acceptance Criteria

### Xác minh Code (Programmatic Verification)
- [ ] Lệnh `dotnet build WebPhotocopyHub.Web.Customer.csproj` phải chạy thành công (không có lỗi cú pháp Razor).
- [ ] Không còn bất kỳ class nào đặc trưng của Bootstrap (như `container`, `row`, `col-md-*`, `btn-primary`, `card`) tồn tại trong các file .cshtml đã được refactor.

### Đảm bảo chất lượng thiết kế (Agent-as-judge)
- [ ] Giao diện các trang mới phải sử dụng đúng bảng màu Tailwind đã được khai báo trong `_BranchCustomerModernLayout.cshtml`.
- [ ] Form submit và các biến Model binding không bị sai lệch hay mất mát so với file gốc.
