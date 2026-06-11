using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.DTOs;

public sealed class ShopDashboardSummaryDto
{
    public int PendingTopUp { get; set; }
    public int PendingAdminTopUp { get; set; }
    public int PrintQueue { get; set; }
    public int ProductOrdersWaiting { get; set; }
    public int SupportOrdersWaiting { get; set; }
    public int LowStockProducts { get; set; }
    public List<Product> LatestLowStockProducts { get; set; } = new();
}
