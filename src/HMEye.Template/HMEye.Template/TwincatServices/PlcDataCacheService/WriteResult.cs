namespace HMEye.TwincatServices
{
	public class WriteResult
	{
		public bool Success { get; }
		public string ErrorMessage { get; }

		public WriteResult(bool success, string errorMessage = "")
		{
			Success = success;
			ErrorMessage = errorMessage;
		}
	}
}
