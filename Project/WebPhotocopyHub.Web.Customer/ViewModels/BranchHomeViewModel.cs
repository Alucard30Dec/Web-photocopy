using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Web.Models;

namespace WebPhotocopyHub.Web.Customer.Models;

public class BranchHomeViewModel
{
    public ShopBranchLinkViewModel Branch { get; set; } = new();

    public List<BranchQuickPriceViewModel> QuickPrices { get; set; } = new();

    public List<PrintJob> RecentPrintJobs { get; set; } = new();

    public int ActivePrintJobCount { get; set; }

    public bool IsSignedIn { get; set; }
}

public class BranchQuickPriceViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
}