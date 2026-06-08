using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Domain.Enums;

namespace PhotoCopyHub.Application.Contracts;

public interface IPrintJobService
{
    Task<PrintJob> CreateAndSubmitAsync(CreatePrintJobDto request, CancellationToken cancellationToken = default);
    Task<List<PrintJob>> GetUserOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<PrintJob>> GetAllOrdersAsync(CancellationToken cancellationToken = default);
    Task<PrintJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, PrintJobStatus status, string actorUserId, bool actorIsAdmin, string? note, CancellationToken cancellationToken = default);
    Task RefundAsync(Guid id, string actorUserId, bool actorIsAdmin, string reason, CancellationToken cancellationToken = default);
}
