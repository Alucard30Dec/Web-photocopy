using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PhotoCopyHub.Application.Common;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;
using PhotoCopyHub.Domain.Constants;
using PhotoCopyHub.Web;
using PhotoCopyHub.Web.Extensions;
using PhotoCopyHub.Web.Models;

namespace PhotoCopyHub.Web.Controllers;

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
            return RedirectToAction(nameof(History));
        }
        catch (BusinessException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> History(CancellationToken cancellationToken)
    {
        var orders = await _supportServiceOrderService.GetUserOrdersAsync(User.GetUserId(), cancellationToken);
        return View(orders);
    }
}
