namespace HMEye.Twincat.Cache.PlcCache;

public interface IPlcCacheItem
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
	IPlcCacheWriteOperation CreateWriteOperation(object value);
}
