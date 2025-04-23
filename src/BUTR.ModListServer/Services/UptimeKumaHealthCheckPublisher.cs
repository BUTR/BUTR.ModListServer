using Microsoft.Extensions.Diagnostics.HealthChecks;

using System.Globalization;

namespace BUTR.ModListServer.Services;

public sealed class UptimeKumaHealthCheckPublisher : IHealthCheckPublisher
{
    private readonly HttpClient _httpClient;

    public UptimeKumaHealthCheckPublisher(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task PublishAsync(HealthReport report, CancellationToken ct)
    {
        if (_httpClient.BaseAddress is null)
            return;
        
        var response = await _httpClient.GetAsync($"?status={(report.Status == HealthStatus.Healthy ? "up" : "down")}&msg={Uri.EscapeDataString(report.Status.ToString())}&ping={report.TotalDuration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}", ct);
        response.EnsureSuccessStatusCode();
    }
}