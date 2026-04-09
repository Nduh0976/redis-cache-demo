using Microsoft.AspNetCore.Mvc;
using RedisCacheDemo.Services;

namespace RedisCacheDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

    [HttpPut("{id:int}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updatedProduct = await _productService.UpdateStockAsync(id, request.NewStock, cancellationToken);
            return Ok(updatedProduct);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

[ApiController]
[Route("cache")]
public class CacheStatsController : ControllerBase
{
    private readonly CacheMetricsService _cacheMetricsService;

    public CacheStatsController(CacheMetricsService cacheMetricsService)
    {
        _cacheMetricsService = cacheMetricsService;
    }

    [HttpGet("stats")]
    public IActionResult Stats()
    {
        var (hits, misses, ratio) = _cacheMetricsService.GetStats();
        return Ok(new { hits, misses, hitRatioPercent = $"{ratio}%" });
    }
}

public record UpdateStockRequest(int NewStock);