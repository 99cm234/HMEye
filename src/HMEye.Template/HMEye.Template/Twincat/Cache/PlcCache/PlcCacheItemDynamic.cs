using HMEye.Twincat.Plc.PlcService;

namespace HMEye.Twincat.Cache.PlcCache;

public class PlcCacheItemDynamic<T> : IPlcCacheItem
{
	public string Address { get; }
	public Type Type => typeof(object);
	private volatile object? _value;
	public DateTime LastUpdated { get; private set; }
	public int? PollInterval { get; }
	public bool IsReadOnly { get; }
	public bool IsDynamic => true;
	public bool IsArray => false;
	private readonly IPlcService _plcService;

	public PlcCacheItemDynamic(string address, int? pollInterval, IPlcService plcService, bool isReadOnly = false)
	{
		Address = address ?? throw new ArgumentNullException(nameof(address));
		PollInterval = pollInterval;
		IsReadOnly = isReadOnly;
		_plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
	}

	public async Task GetAsync()
	{
		var newValue = await _plcService.ReadDynamicAsync<T>(Address);
		_value = newValue;
		LastUpdated = DateTime.UtcNow;
	}

	public async Task SetAsync(object value)
	{
		if (IsReadOnly)
			throw new InvalidOperationException($"Variable {Address} is read-only");

		await _plcService.WriteAsync(Address, value);
		_value = value;
		LastUpdated = DateTime.UtcNow;
	}

	public object? GetValue() => _value;

	public bool IsDueForPolling() =>
		PollInterval.HasValue && DateTime.UtcNow >= LastUpdated + TimeSpan.FromMilliseconds(PollInterval.Value);

	public IPlcCacheWriteOperation CreateWriteOperation(object value)
	{
		return new PlcCacheWriteDynamicOperation(Address, value);
	}
}
