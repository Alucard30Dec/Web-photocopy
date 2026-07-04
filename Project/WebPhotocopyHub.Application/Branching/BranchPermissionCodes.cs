namespace WebPhotocopyHub.Application.Branching;

public static class BranchPermissionCodes
{
    public const string DashboardView = "Dashboard.View";
    public const string PrintJobsView = "PrintJobs.View";
    public const string PrintJobsManage = "PrintJobs.Manage";
    public const string PrintJobsRefund = "PrintJobs.Refund";
    public const string PrintJobsFiles = "PrintJobs.Files";
    public const string ProductOrdersView = "ProductOrders.View";
    public const string ProductOrdersManage = "ProductOrders.Manage";
    public const string ProductOrdersRefund = "ProductOrders.Refund";
    public const string SupportOrdersView = "SupportOrders.View";
    public const string SupportOrdersManage = "SupportOrders.Manage";
    public const string SupportOrdersRefund = "SupportOrders.Refund";
    public const string InventoryView = "Inventory.View";
    public const string InventoryAdjust = "Inventory.Adjust";
    public const string TopUpsView = "TopUps.View";
    public const string TopUpsReview = "TopUps.Review";
    public const string CounterTopUp = "TopUps.Counter";
    public const string ReportsView = "Reports.View";

    public static readonly IReadOnlyList<BranchPermissionDefinition> Definitions = new[]
    {
        new BranchPermissionDefinition(DashboardView, "Tổng quan", "Xem dashboard và chỉ số của cơ sở."),
        new BranchPermissionDefinition(PrintJobsView, "Xem đơn in", "Xem danh sách và chi tiết đơn in của cơ sở."),
        new BranchPermissionDefinition(PrintJobsManage, "Xử lý đơn in", "Nhận, phân công và cập nhật trạng thái đơn in."),
        new BranchPermissionDefinition(PrintJobsRefund, "Hoàn tiền đơn in", "Thực hiện hoàn tiền cho đơn in theo quy trình."),
        new BranchPermissionDefinition(PrintJobsFiles, "Xem/tải file in", "Xem trước và tải file khách hàng đã gửi."),
        new BranchPermissionDefinition(ProductOrdersView, "Xem đơn sản phẩm", "Xem đơn mua sản phẩm tại cơ sở."),
        new BranchPermissionDefinition(ProductOrdersManage, "Xử lý đơn sản phẩm", "Xác nhận, chuẩn bị và cập nhật đơn sản phẩm."),
        new BranchPermissionDefinition(ProductOrdersRefund, "Hoàn tiền đơn sản phẩm", "Thực hiện hoàn tiền đơn mua sản phẩm."),
        new BranchPermissionDefinition(SupportOrdersView, "Xem đơn hỗ trợ", "Xem đơn dịch vụ hỗ trợ tại cơ sở."),
        new BranchPermissionDefinition(SupportOrdersManage, "Xử lý đơn hỗ trợ", "Xử lý và cập nhật đơn dịch vụ hỗ trợ."),
        new BranchPermissionDefinition(SupportOrdersRefund, "Hoàn tiền đơn hỗ trợ", "Thực hiện hoàn tiền đơn dịch vụ hỗ trợ."),
        new BranchPermissionDefinition(InventoryView, "Xem tồn kho", "Xem sản phẩm và lịch sử tồn kho."),
        new BranchPermissionDefinition(InventoryAdjust, "Điều chỉnh tồn kho", "Nhập, xuất và điều chỉnh số lượng tồn kho."),
        new BranchPermissionDefinition(TopUpsView, "Xem giao dịch nạp tiền", "Xem yêu cầu nạp tiền và giao dịch liên quan."),
        new BranchPermissionDefinition(TopUpsReview, "Duyệt nạp tiền", "Chấp nhận hoặc từ chối yêu cầu nạp tiền."),
        new BranchPermissionDefinition(CounterTopUp, "Nạp tiền tại quầy", "Tạo giao dịch nạp tiền trực tiếp tại cơ sở."),
        new BranchPermissionDefinition(ReportsView, "Xem báo cáo", "Xem và xuất báo cáo vận hành của cơ sở.")
    };

    public static readonly IReadOnlyList<string> All = Definitions.Select(x => x.Code).ToArray();
}

public sealed record BranchPermissionDefinition(string Code, string Name, string Description);
