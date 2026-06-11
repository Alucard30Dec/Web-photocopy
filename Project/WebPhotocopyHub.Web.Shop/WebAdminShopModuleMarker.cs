namespace WebPhotocopyHub.Web.Shop;

/// <summary>
/// Module Admin_Shop dùng cho khu quản trị riêng của từng cơ sở/chi nhánh.
/// URL public vẫn giữ dạng /{branchSlug}/Admin để không phá route hiện có.
/// Area MVC bên trong vẫn giữ tên "Shop" nhằm giữ tương thích route và view hiện tại.
/// </summary>
public static class WebAdminShopModuleMarker
{
}