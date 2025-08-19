namespace HMEye.TwincatServices;

public class PlcWriteVariableOperation<T> : IWriteOperation where T : notnull
{
	public string Address { get; }
	public T Value { get; }
	object IWriteOperation.Value => Value!;

	public PlcWriteVariableOperation(string address, T value)
	{
		Address = address ?? throw new ArgumentNullException(nameof(address));
		Value = value;
	}

	public async Task ExecuteAsync(IPlcService plc, CancellationToken ct)
	{
		await plc.WriteAsync(Address, Value, ct);
	}
}
