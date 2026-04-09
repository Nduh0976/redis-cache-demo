namespace RedisCacheDemo.Services
{
    public class CacheMetricsService
    {
        private long _hits;
        private long _misses;

        public void RecordHit()
        {
            Interlocked.Increment(ref _hits);
        }

        public void RecordMiss()
        {
            Interlocked.Increment(ref _misses);
        }

        public (long Hits, long Misses, double HitRatioPercent) GetStats()
        {
            var hits = Interlocked.Read(ref _hits);
            var misses = Interlocked.Read(ref _misses);
            var total = hits + misses;

            var hitRatio = total > 0
                ? Math.Round((double)hits / total * 100, 1)
                : 0;

            return (hits, misses, hitRatio);
        }
    }
}
