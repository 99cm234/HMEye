namespace HMEye.TwincatServices;

public class PlcWriteArrayOperation<T> : IWriteOperation
{
	public string Address { get; }
	public T[] Value { get; }

	public PlcWriteArrayOperation(string address, T[] value)
	{
		Address = address ?? throw new ArgumentNullException(nameof(address));
		Value = value ?? throw new ArgumentNullException(nameof(value));
	}

	public async Task ExecuteAsync(IPlcService plc, CancellationToken ct)
	{
		await plc.WriteArrayAsync(Address, Value, ct);
	}
}
