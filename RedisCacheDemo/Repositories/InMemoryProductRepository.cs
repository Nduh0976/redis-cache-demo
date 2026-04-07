using Bogus;
using RedisCacheDemo.Models;

namespace RedisCacheDemo.Repositories;

public class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<int, Product> _store;

    public InMemoryProductRepository()
    {
        // Seed 200 products with Bogus
        var faker = new Faker<Product>()
            .CustomInstantiator(f => new Product(
                f.IndexFaker + 1,
                f.Commerce.ProductName(),
                decimal.Parse(f.Commerce.Price()),
                f.Commerce.Categories(1)[0],
                f.Random.Int(0, 500)
            ));

        _store = faker
            .Generate(200)
            .ToDictionary(p => p.Id);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate db latency
        return _store.GetValueOrDefault(id);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken); // Simulate heavier db latency
        return _store.Values.ToList();
    }

    public async Task<Product> UpdateStockAsync(int id, int newStock, CancellationToken cancellationToken = default)
    {
        await Task.Delay(30, cancellationToken);

        if (!_store.TryGetValue(id, out var existingProduct))
        {
            throw new KeyNotFoundException($"Product with id {id} was not found.");
        }

        var updatedProduct = existingProduct with { Stock = newStock };
        _store[id] = updatedProduct;
        return updatedProduct;
    }
}
