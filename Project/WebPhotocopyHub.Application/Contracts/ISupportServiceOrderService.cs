using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;

namespace WebPhotocopyHub.Application.Contracts;

public interface ISupportServiceOrderService
{
    Task<List<SupportService>> GetActiveServicesAsync(CancellationToken cancellationToken = default);
    Task<List<SupportService>> GetAllServicesAsync(CancellationToken cancellationToken = default);
    Task<SupportService?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportService> UpsertServiceAsync(SupportService service, CancellationToken cancellationToken = default);
    Task<SupportServiceOrder> CreateOrderAsync(CreateSupportServiceOrderDto request, CancellationToken cancellationToken = default);
    Task<PagedResult<SupportServiceOrder>> GetUserOrdersAsync(string userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<PagedResult<SupportServiceOrder>> GetAllOrdersAsync(int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<SupportServiceOrder?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string actorUserId, string? note, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
