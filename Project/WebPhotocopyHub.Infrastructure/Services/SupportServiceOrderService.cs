using System.Data;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.Infrastructure.Services;

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

    public async Task<PagedResult<SupportServiceOrder>> GetUserOrdersAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SupportServiceOrders
            .AsNoTracking()
            .Include(x => x.SupportService)
            .Where(x => x.UserId == userId);
            
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedResult<SupportServiceOrder>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<SupportServiceOrder>> GetAllOrdersAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SupportServiceOrders
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.SupportService);
            
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedResult<SupportServiceOrder>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Task<SupportServiceOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.SupportServiceOrders
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.SupportService)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);
    }

    public async Task UpdateOrderStatusAsync(
        Guid orderId,
        OrderStatus status,
        string actorUserId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        if (status == OrderStatus.Refunded)
        {
            throw new BusinessException("Vui lòng dùng chức năng hoàn tiền để chuyển sang trạng thái đã hoàn tiền.");
        }

        if (status == OrderStatus.Cancelled)
        {
            await CancelByOperatorAsync(orderId, actorUserId, note ?? "Hủy đơn và hoàn tiền", cancellationToken);
            return;
        }

        var order = await _dbContext.SupportServiceOrders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn dịch vụ.");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            throw new BusinessException("Không thể cập nhật đơn đã hủy hoặc đã hoàn tiền.");
        }

        order.Status = status;
        order.ProcessedByOperatorId = actorUserId;
        order.ProcessedAt = DateTime.UtcNow;
        order.ProcessNote = note;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RefundAsync(Guid orderId, string actorUserId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException("Vui lòng nhập lý do hoàn tiền.");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await _dbContext.SupportServiceOrders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn dịch vụ.");

        if (order.Status == OrderStatus.Refunded)
        {
            throw new BusinessException("Đơn dịch vụ đã được hoàn tiền trước đó.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new BusinessException("Đơn dịch vụ đã hủy không thể hoàn tiền thêm.");
        }

        await _walletService.CreditAsync(new WalletOperationRequestDto
        {
            UserId = order.UserId,
            Amount = order.TotalAmount,
            TransactionType = WalletTransactionType.Refund,
            ReferenceType = nameof(SupportServiceOrder),
            ReferenceId = order.Id,
            Note = reason.Trim(),
            IdempotencyKey = $"supportorder-refund-{order.Id:N}",
            PerformedByAdminId = actorUserId
        }, cancellationToken);

        order.Status = OrderStatus.Refunded;
        order.ProcessedByOperatorId = actorUserId;
        order.ProcessedAt = DateTime.UtcNow;
        order.ProcessNote = reason.Trim();

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "RefundSupportServiceOrder",
            EntityName = nameof(SupportServiceOrder),
            EntityId = order.Id.ToString(),
            Details = reason.Trim()
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task CancelOrderAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await _dbContext.SupportServiceOrders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn dịch vụ.");

        if (order.UserId != userId)
        {
            throw new BusinessException("Bạn không có quyền huỷ đơn này.");
        }

        if (order.Status != OrderStatus.Submitted)
        {
            throw new BusinessException("Chỉ có thể huỷ đơn khi đơn chưa được xử lý.");
        }

        order.Status = OrderStatus.Cancelled;
        order.ProcessNote = "Khách hàng tự huỷ";

        await _walletService.CreditAsync(new WalletOperationRequestDto
        {
            UserId = order.UserId,
            Amount = order.TotalAmount,
            TransactionType = WalletTransactionType.Refund,
            ReferenceType = nameof(SupportServiceOrder),
            ReferenceId = order.Id,
            Note = $"Hoàn tiền huỷ đơn dịch vụ hỗ trợ {order.Id}",
            IdempotencyKey = $"supportorder-cancel-refund-{order.Id:N}",
            PerformedByAdminId = userId
        }, cancellationToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "CancelSupportServiceOrder",
            EntityName = nameof(SupportServiceOrder),
            EntityId = order.Id.ToString(),
            Details = "Khách hàng hủy đơn và hệ thống hoàn tiền."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task CancelByOperatorAsync(Guid orderId, string actorUserId, string reason, CancellationToken cancellationToken)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await _dbContext.SupportServiceOrders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn dịch vụ.");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            throw new BusinessException("Đơn dịch vụ đã hủy hoặc đã hoàn tiền.");
        }

        if (order.Status == OrderStatus.Completed)
        {
            throw new BusinessException("Đơn đã hoàn tất cần dùng chức năng hoàn tiền.");
        }

        await _walletService.CreditAsync(new WalletOperationRequestDto
        {
            UserId = order.UserId,
            Amount = order.TotalAmount,
            TransactionType = WalletTransactionType.Refund,
            ReferenceType = nameof(SupportServiceOrder),
            ReferenceId = order.Id,
            Note = reason,
            IdempotencyKey = $"supportorder-cancel-refund-{order.Id:N}",
            PerformedByAdminId = actorUserId
        }, cancellationToken);

        order.Status = OrderStatus.Cancelled;
        order.ProcessedByOperatorId = actorUserId;
        order.ProcessedAt = DateTime.UtcNow;
        order.ProcessNote = reason;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "CancelSupportServiceOrderByOperator",
            EntityName = nameof(SupportServiceOrder),
            EntityId = order.Id.ToString(),
            Details = reason
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        var trimmed = key?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
