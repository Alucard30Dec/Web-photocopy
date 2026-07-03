using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebPhotocopyHub.Application.Common;
using WebPhotocopyHub.Application.Contracts;
using WebPhotocopyHub.Application.DTOs;
using WebPhotocopyHub.Domain.Constants;
using WebPhotocopyHub.Web;
using WebPhotocopyHub.Web.Extensions;
using WebPhotocopyHub.Web.Customer.Models;

namespace WebPhotocopyHub.Web.Controllers;

[Authorize(Policy = AppPolicies.CustomerPortal)]
public class SupportOrdersController : Controller
{
    private readonly ISupportServiceOrderService _supportServiceOrderService;

    public SupportOrdersController(ISupportServiceOrderService supportServiceOrderService)
    {
        _supportServiceOrderService = supportServiceOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var vm = new CreateSupportOrderViewModel
        {
            AvailableServices = await _supportServiceOrderService.GetActiveServicesAsync(cancellationToken)
        };

        return View(vm);
    }

    [HttpPost]
    [EnableRateLimiting("money")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateSupportOrderViewModel model, CancellationToken cancellationToken)
    {
        model.AvailableServices = await _supportServiceOrderService.GetActiveServicesAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _supportServiceOrderService.CreateOrderAsync(new CreateSupportServiceOrderDto
            {
                UserId = User.GetUserId(),
                SupportServiceId = model.SupportServiceId,
                IdempotencyKey = model.IdempotencyKey,
                Quantity = model.Quantity,
                Notes = model.Notes
            }, cancellationToken);

            TempData["Success"] = "Đặt dịch vụ hỗ trợ thành công.";
            return RedirectToAction(nameof(History), BranchRouteValues());
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> History([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        var orders = await _supportServiceOrderService.GetUserOrdersAsync(User.GetUserId(), page, 10, cancellationToken);
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var item = await _supportServiceOrderService.GetOrderByIdAsync(id, cancellationToken);
        if (item is null || item.UserId != User.GetUserId())
        {
            return NotFound();
        }

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _supportServiceOrderService.CancelOrderAsync(id, User.GetUserId(), cancellationToken);
            TempData["Success"] = "Đã huỷ đơn dịch vụ thành công.";
        }
        catch (BusinessException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id, branchSlug = RouteData.Values["branchSlug"]?.ToString() });
    }

    private object? BranchRouteValues()
    {
        var branchSlug = RouteData.Values["branchSlug"]?.ToString();
        return string.IsNullOrWhiteSpace(branchSlug) ? null : new { branchSlug };
    }
}
