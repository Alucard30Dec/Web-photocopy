using System.Data;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Data;

namespace PhotoCopyHub.Infrastructure.Services;

public class SupportServiceOrderService : ISupportServiceOrderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWalletService _walletService;

    public SupportServiceOrderService(ApplicationDbContext dbContext, IWalletService walletService)
    {
        _dbContext = dbContext;
        _walletService = walletService;
    }

    public Task<List<SupportService>> GetActiveServicesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SupportServices
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<SupportService>> GetAllServicesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SupportServices
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<SupportService?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.SupportServices.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<SupportService> UpsertServiceAsync(SupportService service, CancellationToken cancellationToken = default)
    {
        if (service.Id == Guid.Empty || !await _dbContext.SupportServices.AnyAsync(x => x.Id == service.Id, cancellationToken))
        {
            _dbContext.SupportServices.Add(service);
        }
        else
        {
            _dbContext.SupportServices.Update(service);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return service;
    }

    public async Task<SupportServiceOrder> CreateOrderAsync(CreateSupportServiceOrderDto request, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey) ?? Guid.NewGuid().ToString("N");
        var existingOrder = await _dbContext.SupportServiceOrders
            .Include(x => x.SupportService)
            .FirstOrDefaultAsync(x =>
                x.UserId == request.UserId &&
                x.OrderIdempotencyKey == idempotencyKey, cancellationToken);
        if (existingOrder is not null)
        {
            if (existingOrder.SupportServiceId != request.SupportServiceId
                || existingOrder.Quantity != request.Quantity)
            {
                throw new BusinessException("Idempotency key đã được dùng cho payload khác.");
            }

            return existingOrder;
        }

        if (request.Quantity <= 0)
        {
            throw new BusinessException("Số lượng phải lớn hơn 0.");
        }

        var service = await _dbContext.SupportServices
            .FirstOrDefaultAsync(x => x.Id == request.SupportServiceId && x.IsActive, cancellationToken)
            ?? throw new BusinessException("Dịch vụ hỗ trợ không tồn tại hoặc đã ngừng cung cấp.");

        var total = service.FeeType == SupportFeeType.Fixed
            ? service.UnitPrice
            : service.UnitPrice * request.Quantity;

        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = new SupportServiceOrder
        {
            UserId = request.UserId,
            SupportServiceId = request.SupportServiceId,
            OrderIdempotencyKey = idempotencyKey,
            Quantity = request.Quantity,
            UnitPrice = service.UnitPrice,
            TotalAmount = total,
            Notes = request.Notes,
            Status = OrderStatus.Submitted
        };

        _dbContext.SupportServiceOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _walletService.DebitAsync(new WalletOperationRequestDto
        {
            UserId = request.UserId,
            Amount = total,
            TransactionType = WalletTransactionType.DebitForOrder,
            ReferenceType = nameof(SupportServiceOrder),
            ReferenceId = order.Id,
            Note = $"Thanh toán dịch vụ hỗ trợ {order.Id}",
            IdempotencyKey = idempotencyKey
        }, cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return order;
    }

    public Task<List<SupportServiceOrder>> GetUserOrdersAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.SupportServiceOrders
            .AsNoTracking()
            .Include(x => x.SupportService)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<SupportServiceOrder>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SupportServiceOrders
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.SupportService)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateOrderStatusAsync(
        Guid orderId,
        OrderStatus status,
        string actorUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.SupportServiceOrders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn dịch vụ.");

        order.Status = status;
        order.ProcessedByOperatorId = actorUserId;
        order.ProcessedAt = DateTime.UtcNow;
        order.ProcessNote = note;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        var trimmed = key?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
