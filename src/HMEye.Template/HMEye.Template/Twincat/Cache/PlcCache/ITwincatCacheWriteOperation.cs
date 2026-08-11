using HMEye.TwincatServices.Plc.PlcService;

namespace HMEye.TwincatServices.Cache.PlcCache
{
		public interface ITwincatCacheWriteOperation
	{
		string Address { get; }
		object Value { get; }
		Task ExecuteAsync(ITwincatService plc, CancellationToken ct);
	}
}

