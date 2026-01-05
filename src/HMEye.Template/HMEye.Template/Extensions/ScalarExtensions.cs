using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Scalar.AspNetCore;

namespace HMEye.Extensions;

public static class ScalarExtensions
{
	public static IEndpointRouteBuilder MapHMEyeScalar(this IEndpointRouteBuilder endpoints, IConfiguration configuration)
	{
		var environment = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

		if (environment.IsDevelopment())
		{
			endpoints.MapOpenApi();

			endpoints.MapScalarApiReference(options =>
			{
				options
					.WithTitle("HMEye API")
					.WithTheme(ScalarTheme.Laserwave)
					.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
					.AddPreferredSecuritySchemes("ApiKey")
					.AddApiKeyAuthentication("ApiKey", apiKey =>
					{
						apiKey.Value = configuration["Authentication:ApiKey"];
					});
			});
		}

		return endpoints;
	}
}
