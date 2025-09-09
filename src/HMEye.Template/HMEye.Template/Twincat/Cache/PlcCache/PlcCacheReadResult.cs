namespace HMEye.Twincat.Cache.PlcCache
{
	public class PlcCacheReadResult<T>
	{
		public T? Value { get; }
		public bool Error { get; }
		public string ErrorMessage { get; }

		public PlcCacheReadResult(T? value, bool error = false, string errorMessage = "")
		{
			Value = value;
			Error = error;
			ErrorMessage = errorMessage;
		}
	}
}
