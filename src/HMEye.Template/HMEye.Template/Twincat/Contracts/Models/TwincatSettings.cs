namespace HMEye.Twincat.Contracts.Models
{
	/// <summary>
	/// Used for TwincatServices configuration. Bind to appsettings.json or use in code.
	/// Example appsettings.json:
	/// <code>
	///  "TwincatSettings": {
	///		"NetId": "199.4.42.250.1.1",
	///		"PlcPort": 851,
	///		"Timeout": 10,
	///		"ReconnectDelaySeconds": 5
	///  }
	/// </code>
	/// Example Program.cs:
	/// <code>
	/// builder.Services.Configure<TwincatSettings>(builder.Configuration.GetSection("TwincatSettings"));
	/// </code>
	/// </summary>
	public class TwincatSettings
	{
		public string NetId { get; set; } = string.Empty;
		public int Timeout { get; set; } = 10;
		public int PlcPort { get; set; } = 851;
		public int SystemPort { get; set; } = 10000;
		public int ReconnectDelaySeconds { get; set; } = 5;
	}
}
