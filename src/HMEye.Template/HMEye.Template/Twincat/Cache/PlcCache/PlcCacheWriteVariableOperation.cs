using HMEye.Twincat.Plc.PlcService;

namespace HMEye.Twincat.Cache.PlcCache;

public class PlcCacheWriteVariableOperation<T> : IPlcCacheWriteOperation where T : notnull
{
	public string Address { get; }
	public T Value { get; }
	object IPlcCacheWriteOperation.Value => Value!;

	public PlcCacheWriteVariableOperation(string address, T value)
	{
		Address = address ?? throw new ArgumentNullException(nameof(address));
		Value = value;
	}

	public async Task ExecuteAsync(IPlcService plc, CancellationToken ct)
	{
		await plc.WriteAsync(Address, Value, ct);
	}
}
