using HMEye.TwincatServices.Plc.PlcService;

namespace HMEye.TwincatServices.Cache.PlcCache;

public class TwincatCacheWriteVariableOperation<T> : ITwincatCacheWriteOperation where T : notnull
{
	public string Address { get; }
	public T Value { get; }
	object ITwincatCacheWriteOperation.Value => Value!;

	public TwincatCacheWriteVariableOperation(string address, T value)
	{
		Address = address ?? throw new ArgumentNullException(nameof(address));
		Value = value;
	}

	public async Task ExecuteAsync(ITwincatService plc, CancellationToken ct)
	{
		await plc.WriteAsync(Address, Value, ct);
	}
}

