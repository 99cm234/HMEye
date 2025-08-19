namespace HMEye.TwincatServices;

public static class PlcDataCacheConfigProvider
{
/// <summary>
/// Adds items to Cache without use of the PLC attributes used for PlcDataCacheConfigLoader.
/// Custom structs can be added to cache via this method so they can be cached without use of dynamic types.
/// </summary>
/// <returns></returns>
	public static IEnumerable<CacheItemConfig> GetCacheItemConfigs()
	{
		return new[]
		{
			new CacheItemConfig
			{
				Address = "MAIN.temperature",
				Type = typeof(float),
				PollInterval = 2000,
				IsReadOnly = true,
			},
			new CacheItemConfig
			{
				Address = "MAIN.counter",
				Type = typeof(short),
				PollInterval = 2000,
			},
			new CacheItemConfig
			{
				Address = "MAIN.status",
				Type = typeof(bool),
				PollInterval = 2000,
				IsReadOnly = true,
			},
			new CacheItemConfig
			{
				Address = "MAIN.valuesArray",
				Type = typeof(int), // Element type for int[]
				IsArray = true,
				PollInterval = 2000,
			},
		};
	}
}
