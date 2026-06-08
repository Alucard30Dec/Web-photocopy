namespace PhotoCopyHub.Application.DTOs;

public sealed class AdminDashboardSummaryDto
{
    public int TotalUsers { get; set; }
    public int PendingTopUps { get; set; }
    public int PrintJobsPending { get; set; }
    public int TotalWalletTransactions { get; set; }
    public int ActiveProducts { get; set; }
    public int ActiveSupportServices { get; set; }
}
