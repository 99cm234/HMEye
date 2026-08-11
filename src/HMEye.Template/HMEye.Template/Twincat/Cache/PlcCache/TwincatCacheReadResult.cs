namespace HMEye.TwincatServices.Cache.PlcCache
{
	public class TwincatCacheReadResult<T>
	{
		public T? Value { get; }
		public bool Error { get; }
		public string ErrorMessage { get; }

		public TwincatCacheReadResult(T? value, bool error = false, string errorMessage = "")
		{
			Value = value;
			Error = error;
			ErrorMessage = errorMessage;
		}
	}
}

