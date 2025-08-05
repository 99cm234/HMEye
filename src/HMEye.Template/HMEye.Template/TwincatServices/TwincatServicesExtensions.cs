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

			services.AddSingleton<IPlcDataCache>(sp =>
			{
				var plcService = sp.GetRequiredService<IPlcService>();
				var logger = sp.GetRequiredService<ILogger<PlcDataCache>>();
				var configs = PlcDataCacheConfigProvider.GetCacheItemConfigs();
				return new PlcDataCache(plcService, logger, configs);
			});
			services.AddHostedService<IPlcDataCache>(sp => sp.GetRequiredService<IPlcDataCache>());

			return services;
		}
	}
}
