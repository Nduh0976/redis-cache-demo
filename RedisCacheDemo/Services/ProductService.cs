using RedisCacheDemo.Models;
using RedisCacheDemo.Repositories;

namespace RedisCacheDemo.Services;

public class ProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger _logger;

    private static string ProductKey(int id)
    {
        return $"products:{id}";
    }

    private const string AllProductsKey = "products:all";

    public ProductService(IProductRepository productRepository, ICacheService cacheService, ILogger logger)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = ProductKey(id);

        // Try to get from cache first
        var cachedProduct = await _cacheService.GetAsync<Product>(cacheKey, cancellationToken);
        if (cachedProduct != null)
        {
            _logger.LogInformation("CACHE HIT - key: {Key}", cacheKey);
            return cachedProduct;
        }

        // If not in cache(Cache miss), get from repository
        _logger.LogInformation("CACHE MISS - key: {Key}", cacheKey);
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);


        if (product != null)
        {
            // Store in cache for future requests
            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(10), cancellationToken);
        }

        return product;
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
