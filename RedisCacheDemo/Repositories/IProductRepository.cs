using RedisCacheDemo.Models;

namespace RedisCacheDemo.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Product> UpdateStockAsync(int id, int newStock, CancellationToken cancellationToken = default);
}
