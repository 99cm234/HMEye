using HMEye.Twincat.Plc.PlcService;

namespace HMEye.Twincat.Cache.PlcCache
{
	public class PlcCacheWriteDynamicOperation : IPlcCacheWriteOperation
	{
		public string Address { get; }
		public dynamic Value { get; }

		public PlcCacheWriteDynamicOperation(string address, dynamic value)
		{
			Address = address ?? throw new ArgumentNullException(nameof(address));
			Value = value;
		}

		public async Task ExecuteAsync(IPlcService plc, CancellationToken ct)
		{
			await plc.WriteDynamicAsync(Address, Value, ct);
		}
	}
}
