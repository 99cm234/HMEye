namespace HMEye.TwincatServices.Models;

public class BulkItem
{
	public string Address { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public System.Text.Json.JsonElement Value { get; set; }
}
