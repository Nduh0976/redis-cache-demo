using RedisCacheDemo.Repositories;
using RedisCacheDemo.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Redis - registering the connection multiplexer and cache service
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")!;
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

// IDistributedCache is used by the RedisCacheService to interact with Redis.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "RedisCacheDemo:";
});

// App Services
builder.Services.AddSingleton<CacheMetricsService>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ProductService>();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
