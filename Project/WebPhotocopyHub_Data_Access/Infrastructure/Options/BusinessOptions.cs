namespace PhotoCopyHub.Infrastructure.Options;

public class BusinessOptions
{
    public const string SectionName = "Business";

    public decimal ShippingFee { get; set; } = 15000;
    public decimal TopUpRequireAdminApprovalThreshold { get; set; } = 2000000;
    public decimal RefundRequireAdminApprovalThreshold { get; set; } = 1000000;
}
