namespace HMEye.TwincatServices;

public class PlcWriteVariableOperation<T> : IWriteOperation
{
	public string Address { get; }
	public T Value { get; }

	public PlcWriteVariableOperation(string address, T value)
	{
		Address = address ?? throw new ArgumentNullException(nameof(address));
		Value = value;
	}

	public async Task ExecuteAsync(IPlcService plc, CancellationToken ct)
	{
		await plc.WriteVariableAsync(Address, Value, ct);
	}
}
