using HMEye.TwincatServices.Cache.EventLogCache;
using HMEye.TwincatServices.Cache.PlcCache;
using HMEye.TwincatServices.Contracts.Models;
using HMEye.TwincatServices.Endpoints;
using HMEye.TwincatServices.Plc.EventLogService;
using HMEye.TwincatServices.Plc.PlcService;
using HMEye.TwincatServices.Plc.SystemService;

namespace HMEye.TwincatServices
{
	public static class TwincatServicesExtensions
	{
		public static IServiceCollection AddTwincatServices(
			this IServiceCollection services,
			IConfiguration configuration
		)
		{
			services.Configure<TwincatSettings>(configuration.GetSection("TwincatSettings"));
			services.Configure<TwincatEventLogCacheSettings>(configuration.GetSection("TwincatEventCache"));

			services.AddSingleton<ITwincatService, TwincatService>();
			services.AddHostedService(sp => sp.GetRequiredService<ITwincatService>());

			services.AddSingleton<ITwincatEventLogService, TwincatEventLogService>();
			services.AddHostedService(sp => sp.GetRequiredService<ITwincatEventLogService>());

			services.AddSingleton<ITwincatSystemService, TwincatSystemService>();
			services.AddHostedService(sp => sp.GetRequiredService<ITwincatSystemService>());

			services.AddSingleton<ITwincatEventLogCacheService, TwincatEventLogCacheService>();
			services.AddHostedService(sp => sp.GetRequiredService<ITwincatEventLogCacheService>());

			services.AddTransient<TwincatCacheConfigLoader>();

			services.AddSingleton<ITwincatCache>(sp =>
			{
				var plcService = sp.GetRequiredService<ITwincatService>();
				var logger = sp.GetRequiredService<ILogger<TwincatCache>>();

				var configLoader = sp.GetRequiredService<TwincatCacheConfigLoader>();
				try
				{
					var configs = configLoader.CreateCacheItemConfigs().GetAwaiter().GetResult();
					//var configs = TwincatCacheConfigProvider.GetCacheItemConfigs();
					//var configs = configs1.Concat(configs2);
					return new TwincatCache(plcService, logger, configs);
				}
				catch (OperationCanceledException ex)
				{
					logger.LogError(ex, "Cache configuration loading was canceled. Using empty configuration.");
					return new TwincatCache(plcService, logger, Array.Empty<TwincatCacheItemConfig>());
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to load cache configuration.");
					throw;
				}
			});
			services.AddHostedService(sp => sp.GetRequiredService<ITwincatCache>());

			return services;
		}
		public static IEndpointRouteBuilder MapTwincatEndpoints(this IEndpointRouteBuilder app)
		{
			app.MapTwincatDataEndpoints();

			return app;
		}
	}
}

