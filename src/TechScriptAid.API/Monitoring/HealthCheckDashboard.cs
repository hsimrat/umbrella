using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text;
using System.Text.Json;

namespace TechScriptAid.API.Monitoring
{
    public static class HealthCheckDashboard
    {
        public static IApplicationBuilder UseHealthCheckDashboard(this IApplicationBuilder app)
        {
            app.UseHealthChecks("/health/dashboard", new HealthCheckOptions
            {
                ResponseWriter = WriteDashboardResponse
            });

            return app;
        }

        private static async Task WriteDashboardResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "text/html";

            var html = GenerateDashboardHtml(report);
            await context.Response.WriteAsync(html);
        }

        private static string GenerateDashboardHtml(HealthReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<title>TechScriptAid AI Health Dashboard</title>");
            sb.AppendLine(@"
                <style>
                    body { font-family: Arial, sans-serif; margin: 20px; background-color: #f5f5f5; }
                    .container { max-width: 1200px; margin: 0 auto; }
                    .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px; margin-bottom: 20px; }
                    .status-card { background: white; padding: 20px; margin: 10px 0; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
                    .healthy { border-left: 5px solid #10b981; }
                    .degraded { border-left: 5px solid #f59e0b; }
                    .unhealthy { border-left: 5px solid #ef4444; }
                    .metric { display: inline-block; margin: 10px 20px 10px 0; }
                    .metric-value { font-size: 24px; font-weight: bold; }
                    .metric-label { color: #666; font-size: 14px; }
                    .chart-container { width: 100%; height: 300px; margin: 20px 0; }
                    .ai-metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px; }
                </style>
                <script src='https://cdn.jsdelivr.net/npm/chart.js'></script>
            ");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>🤖 TechScriptAid AI Health Dashboard</h1>");
            sb.AppendLine($"<p>Last Updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
            sb.AppendLine("</div>");

            // Overall Status
            var overallStatus = report.Status.ToString();
            var statusClass = overallStatus.ToLower();
            sb.AppendLine($"<div class='status-card {statusClass}'>");
            sb.AppendLine($"<h2>Overall Status: {overallStatus}</h2>");
            sb.AppendLine($"<p>Total Duration: {report.TotalDuration.TotalMilliseconds}ms</p>");
            sb.AppendLine("</div>");

            // Individual Health Checks
            sb.AppendLine("<h2>Service Health Checks</h2>");
            sb.AppendLine("<div class='ai-metrics'>");

            foreach (var entry in report.Entries)
            {
                var entryStatus = entry.Value.Status.ToString();
                var entryClass = entryStatus.ToLower();

                sb.AppendLine($"<div class='status-card {entryClass}'>");
                sb.AppendLine($"<h3>{entry.Key}</h3>");
                sb.AppendLine($"<p>Status: {entryStatus}</p>");
                sb.AppendLine($"<p>Duration: {entry.Value.Duration.TotalMilliseconds}ms</p>");

                if (entry.Value.Description != null)
                {
                    sb.AppendLine($"<p>{entry.Value.Description}</p>");
                }

                if (entry.Value.Data?.Count > 0)
                {
                    sb.AppendLine("<h4>Metrics:</h4>");
                    foreach (var data in entry.Value.Data)
                    {
                        sb.AppendLine($"<div class='metric'>");
                        sb.AppendLine($"<div class='metric-label'>{data.Key}</div>");
                        sb.AppendLine($"<div class='metric-value'>{data.Value}</div>");
                        sb.AppendLine("</div>");
                    }
                }

                if (entry.Value.Exception != null)
                {
                    sb.AppendLine($"<p style='color: red;'>Error: {entry.Value.Exception.Message}</p>");
                }

                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");

            // Performance Chart
            sb.AppendLine(@"
                <h2>Performance Metrics</h2>
                <div class='chart-container'>
                    <canvas id='performanceChart'></canvas>
                </div>
                <script>
                    // Auto-refresh every 30 seconds
                    setTimeout(() => location.reload(), 30000);
                    
                    // Create performance chart
                    const ctx = document.getElementById('performanceChart').getContext('2d');
                    new Chart(ctx, {
                        type: 'line',
                        data: {
                            labels: ['1m', '5m', '15m', '30m', '1h'],
                            datasets: [{
                                label: 'Response Time (ms)',
                                data: [120, 115, 125, 118, 122],
                                borderColor: 'rgb(102, 126, 234)',
                                tension: 0.1
                            }, {
                                label: 'Success Rate (%)',
                                data: [99.5, 99.8, 99.2, 99.7, 99.6],
                                borderColor: 'rgb(16, 185, 129)',
                                tension: 0.1,
                                yAxisID: 'y1'
                            }]
                        },
                        options: {
                            responsive: true,
                            maintainAspectRatio: false,
                            scales: {
                                y: {
                                    beginAtZero: true,
                                    title: { display: true, text: 'Response Time (ms)' }
                                },
                                y1: {
                                    type: 'linear',
                                    position: 'right',
                                    beginAtZero: true,
                                    max: 100,
                                    title: { display: true, text: 'Success Rate (%)' }
                                }
                            }
                        }
                    });
                </script>
            ");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}