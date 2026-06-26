using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Web.Admin.Models;

namespace WebPhotocopyHub.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? query,
        string? actionName,
        string? entityName,
        string? actorUserId,
        DateTime? fromDate,
        DateTime? toDate,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var allItems = await _auditLogService.GetRecentAsync(1000, cancellationToken);
        var availableActions = allItems
            .Select(x => x.Action)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var availableEntities = allItems
            .Select(x => x.EntityName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        IEnumerable<WebPhotocopyHub.Domain.Entities.AuditLog> filteredItems = allItems;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalizedQuery = query.Trim();
            filteredItems = filteredItems.Where(x =>
                x.Action.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || x.EntityName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || (x.EntityId?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.ActorUserId?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.Details?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.IpAddress?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(actionName))
        {
            filteredItems = filteredItems.Where(x =>
                string.Equals(x.Action, actionName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            filteredItems = filteredItems.Where(x =>
                string.Equals(x.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(actorUserId))
        {
            filteredItems = filteredItems.Where(x =>
                string.Equals(x.ActorUserId, actorUserId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (fromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Local).ToUniversalTime();
            filteredItems = filteredItems.Where(x => x.CreatedAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toExclusiveUtc = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
            filteredItems = filteredItems.Where(x => x.CreatedAt < toExclusiveUtc);
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 20, 100);

        var filteredList = filteredItems.ToList();
        var totalCount = filteredList.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        page = Math.Min(page, totalPages);

        var model = new AuditLogIndexViewModel
        {
            Items = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Query = query,
            ActionName = actionName,
            EntityName = entityName,
            ActorUserId = actorUserId,
            FromDate = fromDate,
            ToDate = toDate,
            AvailableActions = availableActions,
            AvailableEntities = availableEntities,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };

        return View(model);
    }
}
