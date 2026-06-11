using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Web.Customer.Models;

public class DashboardViewModel
{
    public decimal CurrentBalance { get; set; }
    public int PendingTopUpCount { get; set; }
    public int PrintJobsCount { get; set; }
    public int ProductOrdersCount { get; set; }
    public int SupportOrdersCount { get; set; }
    public List<WalletTransaction> RecentTransactions { get; set; } = new();
}
