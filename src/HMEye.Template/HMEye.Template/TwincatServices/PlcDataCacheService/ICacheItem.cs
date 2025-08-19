namespace HMEye.TwincatServices;

public interface ICacheItem
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
	IWriteOperation CreateWriteOperation(object value);
}
