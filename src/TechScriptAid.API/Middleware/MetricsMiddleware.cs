using System.Diagnostics;
using System.Collections.Concurrent;

namespace TechScriptAid.API.Middleware
{
    public class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MetricsMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, MetricData> _metrics = new();

        public MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api/ai"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var metricKey = $"{context.Request.Method}:{context.Request.Path}";

            try
            {
                await _next(context);
                RecordMetric(metricKey, stopwatch.ElapsedMilliseconds, true);
            }
            catch
            {
                RecordMetric(metricKey, stopwatch.ElapsedMilliseconds, false);
                throw;
            }
        }

        private void RecordMetric(string key, long duration, bool success)
        {
            _metrics.AddOrUpdate(key,
                new MetricData { Count = 1, TotalDuration = duration, SuccessCount = success ? 1 : 0 },
                (k, existing) =>
                {
                    existing.Count++;
                    existing.TotalDuration += duration;
                    if (success) existing.SuccessCount++;
                    return existing;
                });

            // Log metrics periodically
            if (_metrics[key].Count % 100 == 0)
            {
                var metric = _metrics[key];
                var avgDuration = metric.TotalDuration / metric.Count;
                var successRate = (metric.SuccessCount * 100.0) / metric.Count;

                _logger.LogInformation(
                    "AI Metrics - Endpoint: {Endpoint}, Calls: {Count}, Avg Duration: {AvgDuration}ms, Success Rate: {SuccessRate}%",
                    key, metric.Count, avgDuration, successRate);
            }
        }

        public static Dictionary<string, object> GetMetrics()
        {
            return _metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)new
                {
                    kvp.Value.Count,
                    AverageDuration = kvp.Value.Count > 0 ? kvp.Value.TotalDuration / kvp.Value.Count : 0,
                    SuccessRate = kvp.Value.Count > 0 ? (kvp.Value.SuccessCount * 100.0) / kvp.Value.Count : 0
                });
        }

        private class MetricData
        {
            public int Count { get; set; }
            public long TotalDuration { get; set; }
            public int SuccessCount { get; set; }
        }
    }
}