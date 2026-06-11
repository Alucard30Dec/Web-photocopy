using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;
using WebPhotocopyHub.Domain.Enums;

namespace WebPhotocopyHub.Application.Contracts;

public interface IPrintJobService
{
    Task<PrintJob> CreateAndSubmitAsync(CreatePrintJobDto request, CancellationToken cancellationToken = default);
    Task<List<PrintJob>> GetUserOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<PrintJob>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
    Task<PrintJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, PrintJobStatus status, string actorUserId, bool actorIsAdmin, string? note, CancellationToken cancellationToken = default);
    Task RefundAsync(Guid id, string actorUserId, bool actorIsAdmin, string reason, CancellationToken cancellationToken = default);
}
