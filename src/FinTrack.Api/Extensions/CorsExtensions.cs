namespace FinTrack.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "FinTrackCorsPolicy";

    public static IServiceCollection AddApiCors(this IServiceCollection services)
    {
        return services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrWhiteSpace(origin)) return false;

                    var uri = new Uri(origin);

                    // 1. Local Angular development
                    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // 2. Production Firebase Hosting domains
                    if (uri.Host.Equals("fintrack-729df.web.app", StringComparison.OrdinalIgnoreCase) ||
                        uri.Host.Equals("fintrack-729df.firebaseapp.com", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // 3. All PR preview channels (https://fintrack-729df--*.web.app)
                    if (uri.Host.StartsWith("fintrack-729df--", StringComparison.OrdinalIgnoreCase) &&
                        (uri.Host.EndsWith(".web.app", StringComparison.OrdinalIgnoreCase) ||
                         uri.Host.EndsWith(".firebaseapp.com", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }

                    return false;
                })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
            });
        });
    }
}
