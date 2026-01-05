namespace HMEye.Twincat.Cache.PlcCache
{
	public class PlcCacheWriteResult
	{
		public bool Success { get; }
		public string ErrorMessage { get; }

		public PlcCacheWriteResult(bool success, string errorMessage = "")
		{
			Success = success;
			ErrorMessage = errorMessage;
		}
	}
}
