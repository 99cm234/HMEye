namespace HMEye.DumbAuth.Models;

public class SessionRecord
{
	public string SessionId { get; set; } = default!;
	public string Username { get; set; } = default!;
	public string ClientIp { get; set; } = default!;
	public string ClientHostName { get; set; } = default!;
	public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
	public DateTime? DisconnectedAt { get; set; }

	public TimeSpan? Duration =>
		DateTime.UtcNow - ConnectedAt;
}
