using HMEye.Twincat.Cache.EventLogCache;
using HMEye.Twincat.Cache.PlcCache;
using HMEye.Twincat.Contracts.Models;
using HMEye.Twincat.Endpoints;
using HMEye.Twincat.Plc.EventLogService;
using HMEye.Twincat.Plc.PlcService;
using HMEye.Twincat.Plc.SystemService;

namespace HMEye.Twincat
{
	public static class TwincatServicesExtensions
	{
		public static IServiceCollection AddTwincatServices(
			this IServiceCollection services,
			IConfiguration configuration
		)
		{
			services.Configure<TwincatSettings>(configuration.GetSection("TwincatSettings"));
			services.Configure<EventLogCacheSettings>(configuration.GetSection("PlcEventCache"));

			services.AddSingleton<IPlcService, PlcService>();
			services.AddHostedService(sp => sp.GetRequiredService<IPlcService>());

			services.AddSingleton<IEventLogService, EventLogService>();
			services.AddHostedService(sp => sp.GetRequiredService<IEventLogService>());

			services.AddSingleton<ISystemService, SystemService>();
			services.AddHostedService(sp => sp.GetRequiredService<ISystemService>());

			services.AddSingleton<IEventLogCacheService, EventLogCacheService>();
			services.AddHostedService(sp => sp.GetRequiredService<IEventLogCacheService>());

			services.AddTransient<PlcCacheConfigLoader>();

			services.AddSingleton<IPlcCache>(sp =>
			{
				var plcService = sp.GetRequiredService<IPlcService>();
				var logger = sp.GetRequiredService<ILogger<PlcCache>>();

				var configLoader = sp.GetRequiredService<PlcCacheConfigLoader>();
				try
				{
					var configs = configLoader.CreateCacheItemConfigs().GetAwaiter().GetResult();
					//var configs = PlcDataCacheConfigProvider.GetCacheItemConfigs();
					//var configs = configs1.Concat(configs2);
					return new PlcCache(plcService, logger, configs);
				}
				catch (OperationCanceledException ex)
				{
					logger.LogError(ex, "Cache configuration loading was canceled. Using empty configuration.");
					return new PlcCache(plcService, logger, Array.Empty<PlcCacheItemConfig>());
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to load cache configuration.");
					throw;
				}
			});
			services.AddHostedService(sp => sp.GetRequiredService<IPlcCache>());

			return services;
		}
		public static IEndpointRouteBuilder MapTwincatEndpoints(this IEndpointRouteBuilder app)
		{
			app.MapPlcDataEndpoints();

			return app;
		}
	}
}
