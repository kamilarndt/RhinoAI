using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RhinoAI.Core
{
    /// <summary>
    /// Performance monitoring system for RhinoAI plugin
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly SimpleLogger _logger;
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
        private readonly System.Threading.Timer _reportingTimer;
        private readonly object _lockObject = new object();

        public PerformanceMonitor(SimpleLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
            
            // Report performance metrics every 5 minutes
            _reportingTimer = new System.Threading.Timer(ReportMetrics, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Start tracking a performance operation
        /// </summary>
        public IDisposable StartTracking(string operationName, string category = "General")
        {
            return new PerformanceTracker(this, operationName, category);
        }

        /// <summary>
        /// Record a performance measurement
        /// </summary>
        public void RecordMeasurement(string operationName, TimeSpan duration, string category = "General", bool success = true)
        {
            var key = $"{category}:{operationName}";
            
            _metrics.AddOrUpdate(key, 
                new PerformanceMetric
                {
                    OperationName = operationName,
                    Category = category,
                    TotalCalls = 1,
                    SuccessfulCalls = success ? 1 : 0,
                    FailedCalls = success ? 0 : 1,
                    TotalDuration = duration,
                    MinDuration = duration,
                    MaxDuration = duration,
                    LastCall = DateTime.Now
                },
                (k, existing) =>
                {
                    lock (_lockObject)
                    {
                        existing.TotalCalls++;
                        if (success)
                            existing.SuccessfulCalls++;
                        else
                            existing.FailedCalls++;
                            
                        existing.TotalDuration = existing.TotalDuration.Add(duration);
                        existing.MinDuration = duration < existing.MinDuration ? duration : existing.MinDuration;
                        existing.MaxDuration = duration > existing.MaxDuration ? duration : existing.MaxDuration;
                        existing.LastCall = DateTime.Now;
                        
                        return existing;
                    }
                });
        }

        /// <summary>
        /// Get performance metrics for a specific operation
        /// </summary>
        public PerformanceMetric? GetMetrics(string operationName, string category = "General")
        {
            var key = $"{category}:{operationName}";
            return _metrics.TryGetValue(key, out var metric) ? metric : null;
        }

        /// <summary>
        /// Get all performance metrics
        /// </summary>
        public IEnumerable<PerformanceMetric> GetAllMetrics()
        {
            return _metrics.Values.ToList();
        }

        /// <summary>
        /// Get performance metrics by category
        /// </summary>
        public IEnumerable<PerformanceMetric> GetMetricsByCategory(string category)
        {
            return _metrics.Values.Where(m => m.Category == category).ToList();
        }

        /// <summary>
        /// Get performance summary
        /// </summary>
        public PerformanceSummary GetSummary()
        {
            var allMetrics = GetAllMetrics().ToList();
            
            return new PerformanceSummary
            {
                TotalOperations = allMetrics.Sum(m => m.TotalCalls),
                TotalSuccessfulOperations = allMetrics.Sum(m => m.SuccessfulCalls),
                TotalFailedOperations = allMetrics.Sum(m => m.FailedCalls),
                AverageSuccessRate = allMetrics.Count > 0 
                    ? allMetrics.Average(m => m.SuccessRate) 
                    : 0,
                TotalDuration = allMetrics.Aggregate(TimeSpan.Zero, (sum, m) => sum.Add(m.TotalDuration)),
                Categories = allMetrics.GroupBy(m => m.Category).Select(g => new CategorySummary
                {
                    Category = g.Key,
                    OperationCount = g.Count(),
                    TotalCalls = g.Sum(m => m.TotalCalls),
                    AverageSuccessRate = g.Average(m => m.SuccessRate),
                    AverageDuration = TimeSpan.FromMilliseconds(g.Average(m => m.AverageDuration.TotalMilliseconds))
                }).ToList()
            };
        }

        /// <summary>
        /// Clear all metrics
        /// </summary>
        public void ClearMetrics()
        {
            _metrics.Clear();
            _logger.LogInformation("Performance metrics cleared");
        }

        /// <summary>
        /// Report performance metrics to log
        /// </summary>
        private void ReportMetrics(object? state)
        {
            try
            {
                var summary = GetSummary();
                
                _logger.LogInformation("=== PERFORMANCE REPORT ===");
                _logger.LogInformation($"Total Operations: {summary.TotalOperations}");
                _logger.LogInformation($"Success Rate: {summary.AverageSuccessRate:F1}%");
                _logger.LogInformation($"Total Duration: {summary.TotalDuration.TotalSeconds:F2}s");
                
                foreach (var category in summary.Categories)
                {
                    _logger.LogInformation($"Category '{category.Category}': {category.TotalCalls} calls, {category.AverageSuccessRate:F1}% success, avg {category.AverageDuration.TotalMilliseconds:F0}ms");
                }
                
                // Report slowest operations
                var slowestOps = GetAllMetrics()
                    .OrderByDescending(m => m.AverageDuration)
                    .Take(5)
                    .ToList();
                    
                if (slowestOps.Any())
                {
                    _logger.LogInformation("Slowest Operations:");
                    foreach (var op in slowestOps)
                    {
                        _logger.LogInformation($"  {op.Category}:{op.OperationName} - Avg: {op.AverageDuration.TotalMilliseconds:F0}ms, Max: {op.MaxDuration.TotalMilliseconds:F0}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error reporting performance metrics: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _reportingTimer?.Dispose();
        }
    }

    /// <summary>
    /// Performance tracker for individual operations
    /// </summary>
    public class PerformanceTracker : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly string _category;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public PerformanceTracker(PerformanceMonitor monitor, string operationName, string category)
        {
            _monitor = monitor;
            _operationName = operationName;
            _category = category;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _monitor.RecordMeasurement(_operationName, _stopwatch.Elapsed, _category, true);
                _disposed = true;
            }
        }

        public void MarkAsFailure()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _monitor.RecordMeasurement(_operationName, _stopwatch.Elapsed, _category, false);
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Performance metric for a specific operation
    /// </summary>
    public class PerformanceMetric
    {
        public string OperationName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan MinDuration { get; set; }
        public TimeSpan MaxDuration { get; set; }
        public DateTime LastCall { get; set; }

        public double SuccessRate => TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls * 100 : 0;
        public TimeSpan AverageDuration => TotalCalls > 0 ? TimeSpan.FromMilliseconds(TotalDuration.TotalMilliseconds / TotalCalls) : TimeSpan.Zero;
    }

    /// <summary>
    /// Overall performance summary
    /// </summary>
    public class PerformanceSummary
    {
        public int TotalOperations { get; set; }
        public int TotalSuccessfulOperations { get; set; }
        public int TotalFailedOperations { get; set; }
        public double AverageSuccessRate { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<CategorySummary> Categories { get; set; } = new List<CategorySummary>();
    }

    /// <summary>
    /// Performance summary by category
    /// </summary>
    public class CategorySummary
    {
        public string Category { get; set; } = string.Empty;
        public int OperationCount { get; set; }
        public int TotalCalls { get; set; }
        public double AverageSuccessRate { get; set; }
        public TimeSpan AverageDuration { get; set; }
    }
} 