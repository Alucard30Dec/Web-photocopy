using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;

namespace WebPhotocopyHub.Application.Contracts;

public interface IProductOrderService
{
    Task<List<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<List<ProductStockMovement>> GetRecentStockMovementsAsync(int take = 200, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AdjustStockAsync(AdjustProductStockDto request, CancellationToken cancellationToken = default);
    Task<Product> UpsertProductAsync(Product product, CancellationToken cancellationToken = default);
    Task<ProductOrder> CreateOrderAsync(CreateProductOrderDto request, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductOrder>> GetUserOrdersAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductOrder>> GetAllOrdersAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<ProductOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string actorUserId, string? note, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
