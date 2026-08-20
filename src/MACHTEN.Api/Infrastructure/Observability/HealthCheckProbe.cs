namespace MACHTEN.Api.Infrastructure.Observability;

/// <summary>
/// Self-contained probe used by the container HEALTHCHECK. The aspnet base
/// image ships neither curl nor wget, so the app checks itself: run the same
/// binary with --healthcheck and it exits 0 only if /health reports healthy.
/// </summary>
public static class HealthCheckProbe
{
    public const string Argument = "--healthcheck";

    public static async Task<int> RunAsync(IConfiguration configuration)
    {
        var port = configuration["ASPNETCORE_HTTP_PORTS"] ?? "8080";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            using var response = await client.GetAsync(new Uri($"http://localhost:{port}/health"));

            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}
