using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Entities;

namespace WebPhotocopyHub.Application.Contracts;

public interface IAuditLogService
{
    Task WriteAsync(AuditLogEntryDto entry, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetRecentAsync(int take = 200, CancellationToken cancellationToken = default);
}
