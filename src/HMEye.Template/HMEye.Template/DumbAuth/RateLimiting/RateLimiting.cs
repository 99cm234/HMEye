using System.Threading.RateLimiting;

namespace HMEye.DumbAuth.RateLimiting;

public static class RateLimitingExtensions
{
	public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddRateLimiter(options =>
		{
			options.OnRejected = (context, token) =>
			{
				var referer = context.HttpContext.Request.Headers.Referer.ToString();
				if (!string.IsNullOrEmpty(referer))
				{
					var uriBuilder = new UriBuilder(referer);
					var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
					query["error"] = "rate_limited";
					uriBuilder.Query = query.ToString();
					context.HttpContext.Response.Redirect(uriBuilder.ToString());
				}
				else
				{
					context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
				}
				return ValueTask.CompletedTask;
			};

			var dumbAuthConfig = configuration.GetSection("DumbAuth");
			var permitLimit = dumbAuthConfig.GetValue<int>("RateLimit:PermitLimitPerMinute", 5);

			options.AddPolicy(
				"LoginPolicy",
				context =>
					RateLimitPartition.GetFixedWindowLimiter(
						partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
						factory: _ => new FixedWindowRateLimiterOptions
						{
							PermitLimit = Math.Max(permitLimit, 1),
							Window = TimeSpan.FromMinutes(1),
							QueueLimit = 0,
						}
					)
			);
		});

		return services;
	}
}
