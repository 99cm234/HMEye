namespace HMEye.TwincatServices
{
	public class ReadResult<T>
	{
		public T? Value { get; }
		public bool Error { get; }
		public string ErrorMessage { get; }

		public ReadResult(T? value, bool error = false, string errorMessage = "")
		{
			Value = value;
			Error = error;
			ErrorMessage = errorMessage;
		}
	}
}
