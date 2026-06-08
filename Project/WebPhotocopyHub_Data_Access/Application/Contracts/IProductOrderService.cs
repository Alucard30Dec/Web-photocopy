using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Application.Contracts;

public interface IProductOrderService
{
    Task<List<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<List<ProductStockMovement>> GetRecentStockMovementsAsync(int take = 200, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AdjustStockAsync(AdjustProductStockDto request, CancellationToken cancellationToken = default);
    Task<Product> UpsertProductAsync(Product product, CancellationToken cancellationToken = default);
    Task<ProductOrder> CreateOrderAsync(CreateProductOrderDto request, CancellationToken cancellationToken = default);
    Task<List<ProductOrder>> GetUserOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<ProductOrder>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string actorUserId, string? note, CancellationToken cancellationToken = default);
}
