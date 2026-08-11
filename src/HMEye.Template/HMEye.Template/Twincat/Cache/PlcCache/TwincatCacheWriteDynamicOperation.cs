using HMEye.TwincatServices.Plc.PlcService;

namespace HMEye.TwincatServices.Cache.PlcCache
{
	public class TwincatCacheWriteDynamicOperation : ITwincatCacheWriteOperation
	{
		public string Address { get; }
		public dynamic Value { get; }

		public TwincatCacheWriteDynamicOperation(string address, dynamic value)
		{
			Address = address ?? throw new ArgumentNullException(nameof(address));
			Value = value;
		}

		public async Task ExecuteAsync(ITwincatService plc, CancellationToken ct)
		{
			await plc.WriteDynamicAsync(Address, Value, ct);
		}
	}
}

