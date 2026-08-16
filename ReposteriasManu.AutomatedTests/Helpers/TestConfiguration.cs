namespace ReposteriasManu.AutomatedTests.Helpers;

internal static class TestConfiguration
{
    public static Uri WebUrl { get; } = CreateUri(
        Environment.GetEnvironmentVariable("REPOSTERIAS_WEB_URL"),
        "http://localhost:52456");

    public static Uri ApiRootUrl { get; } = CreateApiRootUri(
        Environment.GetEnvironmentVariable("REPOSTERIAS_API_URL"),
        "http://localhost:5255");

    public static Uri ApiUrl { get; } = new($"{ApiRootUrl.ToString().TrimEnd('/')}/api/");

    public static Uri DatabaseHealthUrl { get; } = new($"{ApiRootUrl.ToString().TrimEnd('/')}/health/database");

    public static TimeSpan DefaultTimeout { get; } = TimeSpan.FromSeconds(
        int.TryParse(Environment.GetEnvironmentVariable("REPOSTERIAS_TEST_TIMEOUT_SECONDS"), out var seconds)
            ? seconds
            : 15);

    public static bool Headless { get; } =
        bool.TryParse(Environment.GetEnvironmentVariable("REPOSTERIAS_SELENIUM_HEADLESS"), out var headless) &&
        headless;

    private static Uri CreateUri(string? configuredValue, string defaultValue)
    {
        var value = string.IsNullOrWhiteSpace(configuredValue) ? defaultValue : configuredValue.Trim();
        return new Uri(value.TrimEnd('/') + "/");
    }

    private static Uri CreateApiRootUri(string? configuredValue, string defaultValue)
    {
        var uri = CreateUri(configuredValue, defaultValue);
        var value = uri.ToString().TrimEnd('/');

        return value.EndsWith("/api", StringComparison.OrdinalIgnoreCase)
            ? new Uri(value[..^4] + "/")
            : uri;
    }
}
