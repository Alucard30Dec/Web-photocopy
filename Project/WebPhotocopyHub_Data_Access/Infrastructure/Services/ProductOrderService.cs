using System.Data;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;
using PhotoCopyHub.Infrastructure.Data;

namespace PhotoCopyHub.Infrastructure.Services;

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

        if (request.DeliveryMethod == DeliveryMethod.Shipping && string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            throw new BusinessException("Vui lòng nhập địa chỉ giao hàng.");
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

    public Task<List<ProductOrder>> GetUserOrdersAsync(string userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductOrders
            .AsNoTracking()
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ProductOrder>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductOrders
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
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
        var order = await _dbContext.ProductOrders.FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException("Không tìm thấy đơn hàng.");

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
