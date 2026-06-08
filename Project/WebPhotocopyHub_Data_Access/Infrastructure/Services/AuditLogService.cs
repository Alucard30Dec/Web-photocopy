using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Infrastructure.Data;

namespace PhotoCopyHub.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteAsync(AuditLogEntryDto entry, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var previousHash = await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => x.RecordHash)
            .FirstOrDefaultAsync(cancellationToken);

        var log = new AuditLog
        {
            ActorUserId = entry.ActorUserId,
            Action = entry.Action,
            EntityName = entry.EntityName,
            EntityId = entry.EntityId,
            Details = MaskSensitive(entry.Details),
            IpAddress = entry.IpAddress,
            PreviousHash = previousHash,
            CreatedAt = now
        };
        log.RecordHash = ComputeHash(log);

        _dbContext.AuditLogs.Add(log);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<AuditLog>> GetRecentAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        return _dbContext.AuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    private static string ComputeHash(AuditLog log)
    {
        var raw = string.Join('|',
            log.Id,
            log.CreatedAt.Ticks,
            log.ActorUserId,
            log.Action,
            log.EntityName,
            log.EntityId,
            log.Details,
            log.IpAddress,
            log.PreviousHash);

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static string? MaskSensitive(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Regex.Replace(value, @"\b\d{6,}\b", match =>
        {
            var s = match.Value;
            if (s.Length <= 4)
            {
                return "****";
            }

            return $"{s[..2]}****{s[^2..]}";
        });
    }
}
