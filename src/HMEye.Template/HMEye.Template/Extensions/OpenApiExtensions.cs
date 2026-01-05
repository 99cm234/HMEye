using Microsoft.OpenApi.Models;

namespace HMEye.Extensions;

public static class OpenApiExtensions
{
	public static void AddHMEyeOpenApi(this IServiceCollection services)
	{
		services.AddOpenApi(options =>
		{
			options.AddDocumentTransformer((document, context, cancellationToken) =>
			{
				document.Info.Title = "HMEye API";
				document.Info.Version = "v1";

				var securityScheme = new OpenApiSecurityScheme
				{
					Name = "X-API-KEY",
					Type = SecuritySchemeType.ApiKey,
					In = ParameterLocation.Header,
					Description = "API Key needed to access the endpoints"
				};
				document.Components ??= new OpenApiComponents();
				document.Components.SecuritySchemes.Add("ApiKey", securityScheme);

				var requirement = new OpenApiSecurityRequirement
				{
					[new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }] = new List<string>()
				};

				foreach (var path in document.Paths)
				{
					if (path.Key.StartsWith("/api"))
					{
						foreach (var operation in path.Value.Operations)
						{
							operation.Value.Security.Add(requirement);
						}
					}
				}
				return Task.CompletedTask;
			});
		});
	}
}
