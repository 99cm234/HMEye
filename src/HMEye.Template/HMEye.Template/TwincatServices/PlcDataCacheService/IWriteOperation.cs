namespace HMEye.TwincatServices
{
	public interface IWriteOperation
	{
		string Address { get; }
		Task ExecuteAsync(IPlcService plc, CancellationToken ct);
	}
}
