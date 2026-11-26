namespace Voxa.WebApi;

/// <summary>
/// Exposes the version this API was built as, so the PWA can detect when it runs against a different API version.
/// </summary>
public static class AppVersion
{
    public const string HeaderName = "X-App-Version";
    public const string Development = "development";

    /// <summary>
    /// Reads the version from the <c>APP_VERSION</c> setting, which the Docker image sets at build time.
    /// </summary>
    public static string Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var version = configuration["APP_VERSION"]?.Trim();
        return string.IsNullOrWhiteSpace(version) ? Development : version;
    }

    /// <summary>
    /// Adds the version header to every response, including error responses written by the exception handler.
    /// </summary>
    public static IApplicationBuilder UseAppVersionHeader(this IApplicationBuilder app, string version)
        => app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = version;
                return Task.CompletedTask;
            });
            return next(context);
        });
}
