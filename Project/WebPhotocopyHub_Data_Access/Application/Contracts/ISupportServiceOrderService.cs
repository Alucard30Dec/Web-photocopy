using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Application.Contracts;

public interface ISupportServiceOrderService
{
    Task<List<SupportService>> GetActiveServicesAsync(CancellationToken cancellationToken = default);
    Task<List<SupportService>> GetAllServicesAsync(CancellationToken cancellationToken = default);
    Task<SupportService?> GetServiceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SupportService> UpsertServiceAsync(SupportService service, CancellationToken cancellationToken = default);
    Task<SupportServiceOrder> CreateOrderAsync(CreateSupportServiceOrderDto request, CancellationToken cancellationToken = default);
    Task<List<SupportServiceOrder>> GetUserOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<SupportServiceOrder>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
    Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string actorUserId, string? note, CancellationToken cancellationToken = default);
}
