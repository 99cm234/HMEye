namespace HMEye.TwincatServices.Cache.PlcCache;

public interface ITwincatCacheItem
{
	string Address { get; }
	Type Type { get; }
	bool IsReadOnly { get; }
	bool IsArray { get; }
	bool IsDynamic { get; }
	Task GetAsync();
	Task SetAsync(object value);
	object? GetValue();
	bool IsDueForPolling();
	ITwincatCacheWriteOperation CreateWriteOperation(object value);
}

