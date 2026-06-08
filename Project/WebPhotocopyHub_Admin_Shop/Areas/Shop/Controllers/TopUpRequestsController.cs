using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Domain.Entities;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Areas.Shop.Controllers;

[Area("Shop")]
[Authorize(Policy = AppPolicies.ShopOperation)]
public class TopUpRequestsController : Controller
{
    private readonly ITopUpService _topUpService;
    private readonly IAuditLogService _auditLogService;
    private readonly UserManager<ApplicationUser> _userManager;

    public TopUpRequestsController(
        ITopUpService topUpService,
        IAuditLogService auditLogService,
        UserManager<ApplicationUser> userManager)
    {
        _topUpService = topUpService;
        _auditLogService = auditLogService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await _topUpService.GetAllRequestsAsync(cancellationToken);
        return View(items);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.TopUpReview)]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewTopUpViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Dữ liệu không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var actorUserId = User.GetUserId();
        var isAdmin = User.IsInRole(RoleConstants.Admin);

        try
        {
            await _topUpService.ReviewRequestAsync(new ReviewTopUpRequestDto
            {
                TopUpRequestId = model.TopUpRequestId,
                IsApprove = model.IsApprove,
                Note = model.Note,
                IdempotencyKey = model.IdempotencyKey,
                ReviewerUserId = actorUserId,
                IsAdminReviewer = isAdmin
            }, cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = actorUserId,
                Action = model.IsApprove ? "ShopReviewTopUpApprove" : "ShopReviewTopUpReject",
                EntityName = "TopUpRequest",
                EntityId = model.TopUpRequestId.ToString(),
                Details = model.Note,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);

            TempData["Success"] = "Đã xử lý yêu cầu nạp tiền.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    [Authorize(Policy = AppPolicies.CounterTopUp)]
    public async Task<IActionResult> CounterTopUp(CancellationToken cancellationToken)
    {
        var vm = await BuildCounterTopUpViewModelAsync(new CounterTopUpViewModel(), cancellationToken);
        return View(vm);
    }

    [HttpPost]
    [Authorize(Policy = AppPolicies.CounterTopUp)]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CounterTopUp(CounterTopUpViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model = await BuildCounterTopUpViewModelAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var actorUserId = User.GetUserId();
            var topUp = await _topUpService.CreateCounterTopUpAsync(new CreateCounterTopUpDto
            {
                TargetUserId = model.TargetUserId,
                Amount = model.Amount,
                OperatorUserId = actorUserId,
                IdempotencyKey = model.IdempotencyKey,
                Note = model.Note
            }, cancellationToken);

            await _auditLogService.WriteAsync(new AuditLogEntryDto
            {
                ActorUserId = actorUserId,
                Action = "CounterTopUp",
                EntityName = "TopUpRequest",
                EntityId = topUp.Id.ToString(),
                Details = $"Amount: {model.Amount}; Note: {model.Note}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, cancellationToken);

            TempData["Success"] = "Nạp tiền tại quầy thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model = await BuildCounterTopUpViewModelAsync(model, cancellationToken);
            return View(model);
        }
    }

    private async Task<CounterTopUpViewModel> BuildCounterTopUpViewModelAsync(
        CounterTopUpViewModel vm,
        CancellationToken cancellationToken)
    {
        var customers = await _userManager.GetUsersInRoleAsync(RoleConstants.Customer);
        vm.CustomerOptions = customers
            .Where(x => x.IsActive)
            .OrderBy(x => x.Email)
            .Select(x => new SelectListItem(
                $"{x.Email} ({x.FullName})",
                x.Id,
                string.Equals(vm.TargetUserId, x.Id, StringComparison.Ordinal)))
            .ToList();

        return vm;
    }
}
