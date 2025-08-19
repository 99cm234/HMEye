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
			services.Configure<PlcEventCacheSettings>(configuration.GetSection("PlcEventCache"));

			services.AddSingleton<IPlcService, PlcService>();
			services.AddHostedService<IPlcService>(sp => sp.GetRequiredService<IPlcService>());

			services.AddSingleton<IEventLoggerService, EventLoggerService>();
			services.AddHostedService<IEventLoggerService>(sp => sp.GetRequiredService<IEventLoggerService>());

			services.AddSingleton<ISystemService, SystemService>();
			services.AddHostedService<ISystemService>(sp => sp.GetRequiredService<ISystemService>());

			services.AddSingleton<IPlcEventCacheService, PlcEventCacheService>();
			services.AddHostedService<IPlcEventCacheService>(sp => sp.GetRequiredService<IPlcEventCacheService>());

			services.AddTransient<PlcDataCacheConfigLoader>();

			services.AddSingleton<IPlcDataCache>(sp =>
			{
				var plcService = sp.GetRequiredService<IPlcService>();
				var logger = sp.GetRequiredService<ILogger<PlcDataCache>>();

				var configLoader = sp.GetRequiredService<PlcDataCacheConfigLoader>();
				try
				{
					var configs = configLoader.CreateCacheItemConfigs().GetAwaiter().GetResult();
					//var configs = PlcDataCacheConfigProvider.GetCacheItemConfigs();
					//var configs = configs1.Concat(configs2);
					return new PlcDataCache(plcService, logger, configs);
				}
				catch (OperationCanceledException ex)
				{
					logger.LogError(ex, "Cache configuration loading was canceled. Using empty configuration.");
					return new PlcDataCache(plcService, logger, Array.Empty<CacheItemConfig>());
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to load cache configuration.");
					throw;
				}
			});
			services.AddHostedService<IPlcDataCache>(sp => sp.GetRequiredService<IPlcDataCache>());

			return services;
		}
	}
}
