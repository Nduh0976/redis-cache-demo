namespace RedisCacheDemo.Models;


public record Product(
    int Id,
    string Name,
    decimal Price,
    string Category,
    int Stock
);