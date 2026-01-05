using Microsoft.AspNetCore.Authorization;

namespace HMEye.Extensions;

public static class AuthorizationExtensions
{
	public static AuthorizationBuilder AddHMEyePolicies(this AuthorizationBuilder builder, IConfiguration configuration)
	{
		return builder
			.Services.AddAuthorizationBuilder()
			.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"))
			.AddPolicy("RequireViewer", policy => policy.RequireRole("User", "Admin"))
			.AddPolicy("AllowAnonymous", policy => policy.RequireAssertion(_ => true))
			.AddPolicy(
				"GrafanaPolicy",
				policy =>
				{
					policy.RequireAssertion(context =>
					{
						var httpContext = context.Resource as HttpContext;
						// Check for admin role from any source (Cookie or API Key)
						if (context.User.IsInRole("Admin"))
							return true;

						if (httpContext != null)
						{
							if (
								httpContext.Request.Path.StartsWithSegments("/grafana/public-dashboards")
								|| httpContext.Request.Path.StartsWithSegments("/grafana/public")
								|| httpContext.Request.Path.StartsWithSegments("/grafana/api/public")
							)
							{
								return true;
							}
							return context.User.IsInRole("Admin");
						}
						return false;
					});
				}
			)
			.AddPolicy(
				"ApiKeyOrAuthenticated",
				policy =>
				{
					policy.RequireAssertion(context =>
					{
						var httpContext = context.Resource as HttpContext;
						if (httpContext == null)
							return false;

						if (httpContext.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
						{
							var configuredApiKey = configuration["Authentication:ApiKey"];
							if (
								!string.IsNullOrEmpty(configuredApiKey)
								&& configuredApiKey.Equals(extractedApiKey.ToString())
							)
							{
								return true;
							}
						}

						// Fallback to any authenticated user (cookie auth from Blazor)
						return context.User.Identity?.IsAuthenticated == true;
					});
				}
			);
	}
}
