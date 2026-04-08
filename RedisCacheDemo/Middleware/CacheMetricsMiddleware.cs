namespace RedisCacheDemo.Middleware;

// Tracks cache hit/miss counters for the lifetime of the app.
// Exposes them via GET /cache/stats.
public class CacheMetricsMiddleware
{
    private readonly RequestDelegate _next;

    //Thread-safe counters
    private static long _cacheHits;
    private static long _cacheMissHits;

    public static long Hits => Interlocked.Read(ref _cacheHits);
    public static long Misses => Interlocked.Read(ref _cacheMissHits);

    public CacheMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Read the X-Cache-Status header set by the Controller
        if (context.Response.Headers.TryGetValue("X-Cache-Status", out var cacheStatus))
        {
            if (cacheStatus == "HIT")
            {
                Interlocked.Increment(ref _cacheHits);
            }

            if (cacheStatus == "MISS")
            {
                Interlocked.Increment(ref _cacheMissHits);
            }
        }

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }

    public static (long hits, long misses, double ratio) GetStats() 
    {
        var h = Hits; var m = Misses; var total = h + m;
        return (h, m, total == 0 ? 0 : Math.Round((double)h / total * 100, 1));
    }
}
