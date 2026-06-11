using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebPhotocopyHub.Application.Contracts;

namespace WebPhotocopyHub.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/catalog")]
[AllowAnonymous]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public class CatalogApiController : ControllerBase
{
    private readonly IProductOrderService _productOrderService;

    public CatalogApiController(IProductOrderService productOrderService)
    {
        _productOrderService = productOrderService;
    }

    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductSummaryResponse>>> GetActiveProducts(CancellationToken cancellationToken)
    {
        var products = await _productOrderService.GetActiveProductsAsync(cancellationToken);

        var response = products
            .Select(product => new ProductSummaryResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.StockQuantity,
                product.ImageUrl,
                product.IsActive))
            .ToList();

        return Ok(response);
    }
}

public sealed record ProductSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? ImageUrl,
    bool IsActive);
