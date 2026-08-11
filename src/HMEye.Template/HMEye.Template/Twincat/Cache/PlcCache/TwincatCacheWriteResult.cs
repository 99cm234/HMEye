namespace HMEye.TwincatServices.Cache.PlcCache
{
	public class TwincatCacheWriteResult
	{
		public bool Success { get; }
		public string ErrorMessage { get; }

		public TwincatCacheWriteResult(bool success, string errorMessage = "")
		{
			Success = success;
			ErrorMessage = errorMessage;
		}
	}
}

