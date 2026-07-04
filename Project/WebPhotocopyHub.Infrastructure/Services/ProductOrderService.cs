using System.Data;
using Microsoft.EntityFrameworkCore;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;
using WebPhotocopyHub.Infrastructure.Data;

namespace WebPhotocopyHub.Infrastructure.Services;

public class ProductOrderService : IProductOrderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWalletService _walletService;

    public ProductOrderService(ApplicationDbContext dbContext, IWalletService walletService)
    {
        _dbContext = dbContext;
        _walletService = walletService;
    }

    public Task<List<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ProductStockMovement>> GetRecentStockMovementsAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductStockMovements
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.ActorUser)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Product> UpsertProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        if (product.Id == Guid.Empty || !await _dbContext.Products.AnyAsync(x => x.Id == product.Id, cancellationToken))
        {
            _dbContext.Products.Add(product);
        }
        else
        {
            _dbContext.Products.Update(product);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task AdjustStockAsync(AdjustProductStockDto request, CancellationToken cancellationToken = default)
    {
        if (request.QuantityDelta == 0)
        {
            throw new BusinessException("Số lượng điều chỉnh phải khác 0.");
        }

        var product = await _dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy sản phẩm.");

        var before = product.StockQuantity;
        var after = before + request.QuantityDelta;
        if (after < 0)
        {
            throw new BusinessException("Tồn kho không được âm.");
        }

        product.StockQuantity = after;

        _dbContext.ProductStockMovements.Add(new ProductStockMovement
        {
            ProductId = product.Id,
            ActorUserId = request.ActorUserId,
            MovementType = request.QuantityDelta > 0 ? StockMovementType.Restock : StockMovementType.ManualAdjustment,
            QuantityChanged = request.QuantityDelta,
            StockBefore = before,
            StockAfter = after,
            Note = request.Note
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductOrder> CreateOrderAsync(CreateProductOrderDto request, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey) ?? Guid.NewGuid().ToString("N");
        var existingOrder = await _dbContext.ProductOrders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x =>
                x.UserId == request.UserId &&
                x.OrderIdempotencyKey == idempotencyKey, cancellationToken);
        if (existingOrder is not null)
        {
            if (existingOrder.DeliveryMethod != request.DeliveryMethod
                || !string.Equals(existingOrder.DeliveryAddress, request.DeliveryAddress, StringComparison.Ordinal))
            {
                throw new BusinessException("Idempotency key đã được dùng cho payload khác.");
            }

            return existingOrder;
        }

        var validItems = request.Items.Where(x => x.Quantity > 0).ToList();
        if (!validItems.Any())
        {
            throw new BusinessException("Vui lòng chọn ít nhất 1 sản phẩm.");
        }

        if (request.DeliveryMethod == DeliveryMethod.Shipping)
        {
            throw new BusinessException("Giao tận nơi đang tạm khóa. Vui lòng chọn nhận tại tiệm.");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var productIds = validItems.Select(x => x.ProductId).ToList();
        var products = await _dbContext.Products
            .Where(x => productIds.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var order = new ProductOrder
        {
            UserId = request.UserId,
            OrderIdempotencyKey = idempotencyKey,
            DeliveryMethod = request.DeliveryMethod,
            DeliveryAddress = request.DeliveryAddress,
            Notes = request.Notes,
            Status = OrderStatus.Submitted
        };

        decimal total = 0;

        foreach (var item in validItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                throw new BusinessException("Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                throw new BusinessException($"Sản phẩm '{product.Name}' không đủ tồn kho.");
            }

            var beforeStock = product.StockQuantity;
            product.StockQuantity -= item.Quantity;
            var afterStock = product.StockQuantity;
            var line = product.Price * item.Quantity;
            total += line;

            order.Items.Add(new ProductOrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                LineTotal = line
            });

            _dbContext.ProductStockMovements.Add(new ProductStockMovement
            {
                ProductId = product.Id,
                ActorUserId = request.UserId,
                MovementType = StockMovementType.OrderDeduction,
                QuantityChanged = -item.Quantity,
                StockBefore = beforeStock,
                StockAfter = afterStock,
                Note = $"Trừ tồn do đơn hàng {order.Id}"
            });
        }

        order.TotalAmount = total;

        _dbContext.ProductOrders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _walletService.DebitAsync(new WalletOperationRequestDto
        {
            UserId = request.UserId,
            Amount = total,
            TransactionType = WalletTransactionType.DebitForOrder,
            ReferenceType = nameof(ProductOrder),
            ReferenceId = order.Id,
            Note = $"Thanh toán đơn văn phòng phẩm {order.Id}",
            IdempotencyKey = idempotencyKey
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return order;
    }

    public async Task<PagedResult<ProductOrder>> GetUserOrdersAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .Where(x => x.UserId == userId);
            
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedResult<ProductOrder>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<ProductOrder>> GetAllOrdersAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductOrders
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(i => i.Product);
            
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedResult<ProductOrder>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Task<ProductOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductOrders
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
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

        var order = await _dbContext.ProductOrders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn hàng.");

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

        var order = await _dbContext.ProductOrders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn hàng.");

        if (order.Status == OrderStatus.Refunded)
        {
            throw new BusinessException("Đơn hàng đã được hoàn tiền trước đó.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new BusinessException("Đơn hàng đã hủy không thể hoàn tiền thêm.");
        }

        if (order.Status is OrderStatus.Submitted or OrderStatus.Processing)
        {
            RestoreStockForOrder(order, actorUserId, $"Hoàn tồn do hoàn tiền đơn {order.Id}");
        }

        await _walletService.CreditAsync(new WalletOperationRequestDto
        {
            UserId = order.UserId,
            Amount = order.TotalAmount,
            TransactionType = WalletTransactionType.Refund,
            ReferenceType = nameof(ProductOrder),
            ReferenceId = order.Id,
            Note = reason.Trim(),
            IdempotencyKey = $"productorder-refund-{order.Id:N}",
            PerformedByAdminId = actorUserId
        }, cancellationToken);

        order.Status = OrderStatus.Refunded;
        order.ProcessedByOperatorId = actorUserId;
        order.ProcessedAt = DateTime.UtcNow;
        order.ProcessNote = reason.Trim();

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "RefundProductOrder",
            EntityName = nameof(ProductOrder),
            EntityId = order.Id.ToString(),
            Details = reason.Trim()
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task CancelOrderAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await _dbContext.ProductOrders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn hàng.");

        if (order.UserId != userId)
        {
            throw new BusinessException("Bạn không có quyền huỷ đơn này.");
        }

        if (order.Status != OrderStatus.Submitted)
        {
            throw new BusinessException("Chỉ có thể huỷ đơn khi đơn đang chờ xử lý.");
        }

        order.Status = OrderStatus.Cancelled;
        order.ProcessNote = "Khách hàng tự huỷ";
        RestoreStockForOrder(order, userId, $"Hoàn tồn do khách hủy đơn {order.Id}");

        await _walletService.CreditAsync(new WalletOperationRequestDto
        {
            UserId = order.UserId,
            Amount = order.TotalAmount,
            TransactionType = WalletTransactionType.Refund,
            ReferenceType = nameof(ProductOrder),
            ReferenceId = order.Id,
            Note = $"Hoàn tiền huỷ đơn văn phòng phẩm {order.Id}",
            IdempotencyKey = $"productorder-cancel-refund-{order.Id:N}",
            PerformedByAdminId = userId
        }, cancellationToken);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "CancelProductOrder",
            EntityName = nameof(ProductOrder),
            EntityId = order.Id.ToString(),
            Details = "Khách hàng hủy đơn và hệ thống hoàn tiền, trả tồn kho."
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task CancelByOperatorAsync(Guid orderId, string actorUserId, string reason, CancellationToken cancellationToken)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var order = await _dbContext.ProductOrders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn hàng.");

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded)
        {
            throw new BusinessException("Đơn hàng đã hủy hoặc đã hoàn tiền.");
        }

        if (order.Status == OrderStatus.Completed)
        {
            throw new BusinessException("Đơn đã hoàn tất cần dùng chức năng hoàn tiền.");
        }

        RestoreStockForOrder(order, actorUserId, $"Hoàn tồn do hủy đơn {order.Id}");

        await _walletService.CreditAsync(new WalletOperationRequestDto
        {
            UserId = order.UserId,
            Amount = order.TotalAmount,
            TransactionType = WalletTransactionType.Refund,
            ReferenceType = nameof(ProductOrder),
            ReferenceId = order.Id,
            Note = reason,
            IdempotencyKey = $"productorder-cancel-refund-{order.Id:N}",
            PerformedByAdminId = actorUserId
        }, cancellationToken);

        order.Status = OrderStatus.Cancelled;
        order.ProcessedByOperatorId = actorUserId;
        order.ProcessedAt = DateTime.UtcNow;
        order.ProcessNote = reason;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = "CancelProductOrderByOperator",
            EntityName = nameof(ProductOrder),
            EntityId = order.Id.ToString(),
            Details = reason
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private void RestoreStockForOrder(ProductOrder order, string actorUserId, string note)
    {
        foreach (var item in order.Items)
        {
            var product = item.Product ?? throw new BusinessException("Không tìm thấy sản phẩm trong đơn hàng.");
            var beforeStock = product.StockQuantity;
            product.StockQuantity += item.Quantity;

            _dbContext.ProductStockMovements.Add(new ProductStockMovement
            {
                ProductId = product.Id,
                ActorUserId = actorUserId,
                MovementType = StockMovementType.Restock,
                QuantityChanged = item.Quantity,
                StockBefore = beforeStock,
                StockAfter = product.StockQuantity,
                Note = note
            });
        }
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        var trimmed = key?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
