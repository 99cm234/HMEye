
namespace HMEye.Twincat.Endpoints;

public class ApiKeyEndpointFilter : IEndpointFilter
{
	private readonly IConfiguration _configuration;
	private const string ApiKeyHeaderName = "X-API-KEY";

	public ApiKeyEndpointFilter(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
	{
		// 1. Check for valid API Key
		if (context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
		{
			var configuredApiKey = _configuration["Authentication:ApiKey"];
			if (!string.IsNullOrEmpty(configuredApiKey) && configuredApiKey.Equals(extractedApiKey))
			{
				return await next(context);
			}
		}

		// 2. Fallback: Check if user is already authenticated (e.g. via Cookies from Blazor)
		if (context.HttpContext.User.Identity?.IsAuthenticated == true)
		{
            // Optional: Check specific roles if needed, e.g. context.HttpContext.User.IsInRole("Admin")
			// For now, any authenticated user is allowed as per "RequireViewer" intent
			return await next(context);
		}

		// 3. Unauthorized
		return Results.Unauthorized();
	}
}
