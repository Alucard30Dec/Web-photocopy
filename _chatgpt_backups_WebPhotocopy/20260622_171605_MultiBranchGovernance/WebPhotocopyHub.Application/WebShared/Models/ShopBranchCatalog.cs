namespace WebPhotocopyHub.Web.Models;

public static class ShopBranchCatalog
{
    private static readonly IReadOnlyList<ShopBranchLinkViewModel> Branches = new List<ShopBranchLinkViewModel>
    {
        new()
        {
            Slug = "ToanPhotocopy",
            Name = "Toàn Photocopy",
            Address = "Cơ sở photocopy Toàn - cập nhật địa chỉ thật trong phần quản trị",
            ShortDescription = "Trang cơ sở dành cho khách hàng gửi file, tạo đơn in, xem sản phẩm văn phòng phẩm và theo dõi trạng thái xử lý tại Toàn Photocopy.",
            PhoneNumber = "Đang cập nhật",
            OpenHours = "08:00 - 21:00 hằng ngày",
            CustomerNote = "Bạn có thể upload file trước, ghi chú số bản, in màu/in trắng đen, một mặt/hai mặt và thời gian muốn nhận.",
            PopularServices = new[]
            {
                "In tài liệu A4/A3, in màu và trắng đen",
                "Photocopy giáo trình, hồ sơ, biểu mẫu",
                "Đóng gáy, bấm kim, phân tập tài liệu",
                "Scan tài liệu và hỗ trợ chỉnh file cơ bản"
            },
            QuickOptions = new[]
            {
                "Upload file online",
                "Ghi chú yêu cầu in",
                "Theo dõi trạng thái đơn",
                "Nhận tài liệu tại tiệm"
            }
        },
        new()
        {
            Slug = "141-dien-bien-phu",
            Name = "WebPhotocopyHub 141 Điện Biên Phủ",
            Address = "141 Điện Biên Phủ",
            ShortDescription = "Cơ sở phục vụ khách hàng đặt in, upload file, đặt sản phẩm và theo dõi trạng thái đơn.",
            PhoneNumber = "Đang cập nhật",
            OpenHours = "08:00 - 21:00",
            CustomerNote = "Khách hàng gửi file trước để cơ sở kiểm tra và chuẩn bị đơn nhanh hơn.",
            PopularServices = new[]
            {
                "In và photocopy tài liệu",
                "Upload file online",
                "Đóng gáy và hoàn thiện"
            },
            QuickOptions = new[]
            {
                "Tạo đơn in",
                "Xem sản phẩm",
                "Dịch vụ hỗ trợ"
            }
        },
        new()
        {
            Slug = "co-so-trung-tam",
            Name = "WebPhotocopyHub Cơ sở trung tâm",
            Address = "Khu vực trung tâm",
            ShortDescription = "Cơ sở phục vụ khách hàng tại khu vực trung tâm, hỗ trợ đặt in, photocopy và đặt sản phẩm.",
            PhoneNumber = "Đang cập nhật",
            OpenHours = "08:00 - 21:00",
            CustomerNote = "Khách hàng có thể gửi file trước, ghi chú yêu cầu và theo dõi trạng thái xử lý trực tuyến.",
            PopularServices = new[]
            {
                "In tài liệu",
                "Photocopy",
                "Đặt sản phẩm"
            },
            QuickOptions = new[]
            {
                "Link khách hàng riêng",
                "Khu quản trị riêng",
                "Tách dữ liệu theo cơ sở"
            }
        }
    };

    public static IReadOnlyList<ShopBranchLinkViewModel> All => Branches;

    public static ShopBranchLinkViewModel? Find(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        return Branches.FirstOrDefault(x =>
            string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsKnownSlug(string? slug)
    {
        return Find(slug) is not null;
    }
}
