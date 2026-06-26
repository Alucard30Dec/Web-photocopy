namespace WebPhotocopyHub.Application.Branching;

public static class BranchFeatureCodes
{
    public const string PrintOrders = "PRINT_ORDERS";
    public const string ProductSales = "PRODUCT_SALES";
    public const string SupportServices = "SUPPORT_SERVICES";
    public const string Inventory = "INVENTORY";
    public const string Pricing = "PRICING";
    public const string TopUps = "TOP_UPS";
    public const string Wallet = "WALLET";
    public const string Reports = "REPORTS";

    public static readonly IReadOnlyList<BranchFeatureDefinition> All = new[]
    {
        new BranchFeatureDefinition(PrintOrders, "Đơn in", "Tiếp nhận, xem file, xử lý trạng thái và hoàn tiền đơn in."),
        new BranchFeatureDefinition(ProductSales, "Bán sản phẩm", "Danh mục sản phẩm và đơn văn phòng phẩm."),
        new BranchFeatureDefinition(SupportServices, "Dịch vụ hỗ trợ", "Scan, đóng gáy, ép plastic và các dịch vụ phụ trợ."),
        new BranchFeatureDefinition(Inventory, "Kho hàng", "Tồn kho, nhập/xuất và điều chỉnh kho."),
        new BranchFeatureDefinition(Pricing, "Bảng giá", "Quy tắc tính giá in riêng của cơ sở."),
        new BranchFeatureDefinition(TopUps, "Nạp tiền", "Duyệt nạp tiền online và nạp tiền tại quầy."),
        new BranchFeatureDefinition(Wallet, "Giao dịch ví", "Xem giao dịch và đối soát số dư phát sinh tại cơ sở."),
        new BranchFeatureDefinition(Reports, "Báo cáo", "Báo cáo vận hành, doanh thu và đối soát theo cơ sở.")
    };
}

public sealed record BranchFeatureDefinition(string Code, string Name, string Description);
