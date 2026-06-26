using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Web.Models;

public static class ShopBranchCatalog
{
    private static readonly object SyncRoot = new();
    private static List<ShopBranchLinkViewModel> _branches = BuildFallbackBranches();

    public static IReadOnlyList<ShopBranchLinkViewModel> All
    {
        get
        {
            lock (SyncRoot)
            {
                return _branches.Select(Clone).ToList();
            }
        }
    }

    public static ShopBranchLinkViewModel? Find(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        lock (SyncRoot)
        {
            var item = _branches.FirstOrDefault(x =>
                string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase));
            return item is null ? null : Clone(item);
        }
    }

    public static bool IsKnownSlug(string? slug)
    {
        return Find(slug) is not null;
    }

    public static void ReplaceFromEntities(IEnumerable<Branch> branches)
    {
        var mapped = branches
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(Map)
            .ToList();

        lock (SyncRoot)
        {
            _branches = mapped.Count > 0 ? mapped : BuildFallbackBranches();
        }
    }

    private static ShopBranchLinkViewModel Map(Branch branch)
    {
        return new ShopBranchLinkViewModel
        {
            Slug = branch.Slug,
            Name = branch.Name,
            Address = branch.Address ?? "Đang cập nhật",
            ShortDescription = branch.ShortDescription ?? $"Cơ sở {branch.Name} phục vụ đặt in, photocopy và các dịch vụ hỗ trợ.",
            PhoneNumber = branch.PhoneNumber ?? "Đang cập nhật",
            OpenHours = branch.OpenHours ?? "Đang cập nhật",
            CustomerNote = branch.CustomerNote ?? "Khách hàng có thể gửi file trước và theo dõi trạng thái xử lý trực tuyến.",
            PopularServices = SplitLines(branch.PopularServices, new[] { "In và photocopy tài liệu", "Upload file online", "Hoàn thiện tài liệu" }),
            QuickOptions = SplitLines(branch.QuickOptions, new[] { "Tạo đơn in", "Theo dõi trạng thái", "Liên hệ cơ sở" })
        };
    }

    private static IReadOnlyList<string> SplitLines(string? value, IReadOnlyList<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value
            .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ShopBranchLinkViewModel Clone(ShopBranchLinkViewModel source)
    {
        return new ShopBranchLinkViewModel
        {
            Slug = source.Slug,
            Name = source.Name,
            Address = source.Address,
            ShortDescription = source.ShortDescription,
            PhoneNumber = source.PhoneNumber,
            OpenHours = source.OpenHours,
            CustomerNote = source.CustomerNote,
            PopularServices = source.PopularServices.ToArray(),
            QuickOptions = source.QuickOptions.ToArray()
        };
    }

    private static List<ShopBranchLinkViewModel> BuildFallbackBranches()
    {
        return new List<ShopBranchLinkViewModel>
        {
            new()
            {
                Slug = "toanphotocopy",
                Name = "Toàn Photocopy",
                Address = "Đang cập nhật",
                ShortDescription = "Cơ sở photocopy phục vụ gửi file, tạo đơn in và theo dõi trạng thái xử lý.",
                PhoneNumber = "Đang cập nhật",
                OpenHours = "08:00 - 21:00 hằng ngày",
                CustomerNote = "Bạn có thể upload file trước và ghi chú đầy đủ yêu cầu in.",
                PopularServices = new[] { "In tài liệu A4/A3", "Photocopy", "Đóng gáy", "Scan tài liệu" },
                QuickOptions = new[] { "Upload file online", "Tạo đơn in", "Theo dõi trạng thái" }
            }
        };
    }
}
