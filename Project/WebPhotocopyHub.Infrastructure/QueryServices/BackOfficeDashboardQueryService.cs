using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.DataAccess.Controllers;

public sealed class BackOfficeDashboardQueryService : IBackOfficeDashboardQueryService
{
    private readonly ApplicationDbContext _dbContext;

    public BackOfficeDashboardQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardSummaryDto> GetAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        return new AdminDashboardSummaryDto
        {
            TotalUsers = await _dbContext.Users.CountAsync(cancellationToken),
            PendingTopUps = await _dbContext.TopUpRequests.CountAsync(
                x => x.Status == TopUpStatus.Pending || x.Status == TopUpStatus.PendingAdminApproval,
                cancellationToken),
            PrintJobsPending = await _dbContext.PrintJobs.CountAsync(
                x => x.Status == PrintJobStatus.Submitted
                     || x.Status == PrintJobStatus.ConfirmedByShop
                     || x.Status == PrintJobStatus.Paid
                     || x.Status == PrintJobStatus.Processing,
                cancellationToken),
            TotalWalletTransactions = await _dbContext.WalletTransactions.CountAsync(cancellationToken),
            ActiveProducts = await _dbContext.Products.CountAsync(x => x.IsActive, cancellationToken),
            ActiveSupportServices = await _dbContext.SupportServices.CountAsync(x => x.IsActive, cancellationToken)
        };
    }

    public async Task<ShopDashboardSummaryDto> GetShopSummaryAsync(CancellationToken cancellationToken = default)
    {
        var v_arrLatestLowStockProducts = await _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.StockQuantity <= 10)
            .OrderBy(x => x.StockQuantity)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new ShopDashboardSummaryDto
        {
            PendingTopUp = await _dbContext.TopUpRequests.CountAsync(x => x.Status == TopUpStatus.Pending, cancellationToken),
            PendingAdminTopUp = await _dbContext.TopUpRequests.CountAsync(x => x.Status == TopUpStatus.PendingAdminApproval, cancellationToken),
            PrintQueue = await _dbContext.PrintJobs.CountAsync(
                x => x.Status == PrintJobStatus.Submitted
                     || x.Status == PrintJobStatus.ConfirmedByShop
                     || x.Status == PrintJobStatus.Paid
                     || x.Status == PrintJobStatus.Processing,
                cancellationToken),
            ProductOrdersWaiting = await _dbContext.ProductOrders.CountAsync(
                x => x.Status == OrderStatus.Submitted || x.Status == OrderStatus.Processing,
                cancellationToken),
            SupportOrdersWaiting = await _dbContext.SupportServiceOrders.CountAsync(
                x => x.Status == OrderStatus.Submitted || x.Status == OrderStatus.Processing,
                cancellationToken),
            LowStockProducts = await _dbContext.Products.CountAsync(x => x.IsActive && x.StockQuantity <= 10, cancellationToken),
            LatestLowStockProducts = v_arrLatestLowStockProducts
        };
    }
}
