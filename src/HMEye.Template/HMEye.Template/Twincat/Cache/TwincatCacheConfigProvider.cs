using HMEye.TwincatServices.Cache.PlcCache;

namespace HMEye.TwincatServices.Cache;

public static class TwincatCacheConfigProvider
{
/// <summary>
/// Adds items to Cache without use of the PLC attributes used for `TwincatCacheConfigLoader`.
/// Custom structs can be added to cache via this method so they can be cached without use of dynamic types.
/// </summary>
/// <returns></returns>
	public static IEnumerable<TwincatCacheItemConfig> GetCacheItemConfigs()
	{
		return new[]
		{
			new TwincatCacheItemConfig
			{
				Address = "MAIN.temperature",
				Type = typeof(float),
				PollInterval = 2000,
				IsReadOnly = true,
			},
			new TwincatCacheItemConfig
			{
				Address = "MAIN.counter",
				Type = typeof(short),
				PollInterval = 2000,
			},
			new TwincatCacheItemConfig
			{
				Address = "MAIN.status",
				Type = typeof(bool),
				PollInterval = 2000,
				IsReadOnly = true,
			},
			new TwincatCacheItemConfig
			{
				Address = "MAIN.valuesArray",
				Type = typeof(int), // Element type for int[]
				IsArray = true,
				PollInterval = 2000,
			},
		};
	}
}

