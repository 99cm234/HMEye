using HMEye.Twincat.Plc.PlcService;

namespace HMEye.Twincat.Cache.PlcCache
{
	public interface IPlcCacheWriteOperation
	{
		string Address { get; }
		object Value { get; }
		Task ExecuteAsync(IPlcService plc, CancellationToken ct);
	}
}
