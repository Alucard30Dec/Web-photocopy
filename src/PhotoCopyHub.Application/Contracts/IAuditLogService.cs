using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;

namespace PhotoCopyHub.Application.Contracts;

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntryDto entry, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetRecentAsync(int take = 200, CancellationToken cancellationToken = default);
}
