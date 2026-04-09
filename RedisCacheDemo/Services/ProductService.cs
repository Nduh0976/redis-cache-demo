using RedisCacheDemo.Models;
using RedisCacheDemo.Repositories;

namespace RedisCacheDemo.Services;

public class ProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ProductService> _logger;

    private static string ProductKey(int id)
    {
        return $"products:{id}";
    }

    private const string AllProductsKey = "products:all";

    public ProductService(IProductRepository productRepository, ICacheService cacheService, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _cacheService.GetOrCreateAsync(ProductKey(id), async c =>
        {
            _logger.LogInformation("CACHE MISS - key: {Key}", ProductKey(id));
            return await _productRepository.GetByIdAsync(id, c);
        }, TimeSpan.FromMinutes(10), cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cachedProducts = await _cacheService.GetAsync<IReadOnlyList<Product>>(AllProductsKey, cancellationToken);
        if (cachedProducts != null)
        {
            _logger.LogInformation("CACHE HIT - key: {Key}", AllProductsKey);
            return cachedProducts;
        }

        _logger.LogInformation("CACHE MISS - key: {Key}", AllProductsKey);
        var products = await _productRepository.GetAllAsync(cancellationToken);
        
        await _cacheService.SetAsync(AllProductsKey, products, TimeSpan.FromMinutes(2), cancellationToken);
        return products;
    }

    public async Task<Product> UpdateStockAsync(int id, int newStock, CancellationToken cancellationToken = default)
    {
        var updatedProduct = await _productRepository.UpdateStockAsync(id, newStock, cancellationToken);

        // Invalidate(bust) relevant cache entries
        await _cacheService.RemoveAsync(ProductKey(id), cancellationToken);
        await _cacheService.RemoveAsync(AllProductsKey, cancellationToken);

        _logger.LogInformation("CACHE BUST - product {Id} updated, cache cleared", id);
        return updatedProduct;
    }
}
