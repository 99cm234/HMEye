namespace HMEye.TwincatServices.PlcDataCacheService
{
	public class PlcWriteDynamicOperation : IWriteOperation
	{
		public string Address { get; }
		public dynamic Value { get; }

		public PlcWriteDynamicOperation(string address, dynamic value)
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
