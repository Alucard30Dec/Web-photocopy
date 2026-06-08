using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotoCopyHub.Application.Contracts;
using PhotoCopyHub.Application.DTOs;

namespace PhotoCopyHub.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/pricing")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public class PricingApiController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public PricingApiController(IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    [HttpGet("rules")]
    [ProducesResponseType(typeof(IReadOnlyList<PricingRuleResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PricingRuleResponse>>> GetActiveRules(CancellationToken cancellationToken)
    {
        var rules = await _pricingService.GetActiveRulesAsync(cancellationToken);

        var response = rules
            .Select(rule => new PricingRuleResponse(
                rule.Id,
                rule.PaperSize.ToString(),
                rule.PrintSide.ToString(),
                rule.ColorMode.ToString(),
                rule.IsPhoto,
                rule.UnitPrice,
                rule.IsActive))
            .ToList();

        return Ok(response);
    }

    [HttpPost("calculate")]
    [ProducesResponseType(typeof(PricingCalculationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PricingCalculationResultDto>> Calculate(
        [FromBody] PricingCalculationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Copies <= 0)
        {
            ModelState.AddModelError(nameof(request.Copies), "Số bản in phải lớn hơn 0.");
        }

        if (request.TotalPages <= 0)
        {
            ModelState.AddModelError(nameof(request.TotalPages), "Tổng số trang phải lớn hơn 0.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _pricingService.CalculatePrintPriceAsync(request, cancellationToken);
        return Ok(result);
    }
}

public sealed record PricingRuleResponse(
    Guid Id,
    string PaperSize,
    string PrintSide,
    string ColorMode,
    bool IsPhoto,
    decimal UnitPrice,
    bool IsActive);
