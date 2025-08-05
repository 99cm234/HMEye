namespace HMEye.TwincatServices;

public interface ICacheItem
{
	string Address { get; }
	Type Type { get; }
	bool IsReadOnly { get; }
	Task GetAsync();
	Task SetValueAsync(object value);
	object? GetValue();
	bool IsDueForPolling();
	IWriteOperation CreateWriteOperation(object value);
}
