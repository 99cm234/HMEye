namespace HMEye.TwincatServices
{
	public interface IWriteOperation
	{
		string Address { get; }
		object Value { get; }
		Task ExecuteAsync(IPlcService plc, CancellationToken ct);
	}
}
