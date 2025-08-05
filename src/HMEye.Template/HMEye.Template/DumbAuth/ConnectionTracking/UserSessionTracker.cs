using System.Collections.Concurrent;
using HMEye.DumbAuth.Models;

namespace HMEye.DumbAuth.ConnectionTracking;

public class UserSessionTracker
{
	private readonly ILogger<UserSessionTracker> _logger;

	// Keyed by (SessionId, Username)
	private readonly ConcurrentDictionary<(string SessionId, string Username), SessionRecord> _sessions = new();

	public event Action? OnSessionRecordChanged;

	public UserSessionTracker(ILogger<UserSessionTracker> logger)
	{
		_logger = logger;
	}

	public void Add(SessionRecord record)
	{
		if (string.IsNullOrWhiteSpace(record.SessionId) || string.IsNullOrWhiteSpace(record.Username))
		{
			// _logger.LogWarning("Attempted to add session with missing SessionId or Username.");
			return;
		}

		var session = _sessions.GetOrAdd(
			(record.SessionId, record.Username),
			_ =>
			{
				_logger.LogInformation(
					"New session added: {Username} ({SessionId}) from IP {Ip}, Host {Host}",
					record.Username,
					record.SessionId,
					record.ClientIp,
					record.ClientHostName
				);

				return new SessionRecord
				{
					SessionId = record.SessionId,
					Username = record.Username,
					ClientIp = record.ClientIp,
					ClientHostName = record.ClientHostName,
					ConnectedAt = DateTime.UtcNow,
				};
			}
		);

		lock (session)
		{
			session.DisconnectedAt = null;
			_logger.LogDebug("Session refreshed: {Username} ({SessionId})", record.Username, record.SessionId);
		}
		OnSessionRecordChanged?.Invoke();
	}

	public void Remove(SessionRecord record)
	{
		if (string.IsNullOrWhiteSpace(record.SessionId) || string.IsNullOrWhiteSpace(record.Username))
		{
			// _logger.LogWarning("Attempted to remove session with missing SessionId or Username.");
			return;
		}

		if (_sessions.TryGetValue((record.SessionId, record.Username), out var session))
		{
			lock (session)
			{
				session.DisconnectedAt = DateTime.UtcNow;
			}
			_logger.LogInformation("Session disconnected: {Username} ({SessionId})", record.Username, record.SessionId);
		}
		CleanupOldSessions();
		OnSessionRecordChanged?.Invoke();
	}

	private void CleanupOldSessions()
	{
		var cutoff = DateTime.UtcNow.AddHours(-1);
		var expiredKeys = _sessions
			.Where(pair => pair.Value.DisconnectedAt is not null && pair.Value.DisconnectedAt < cutoff)
			.Select(pair => pair.Key)
			.ToList();

		foreach (var key in expiredKeys)
		{
			_sessions.TryRemove(key, out _);
		}

		if (expiredKeys.Count > 0)
		{
			_logger.LogInformation("Removed {Count} expired session(s).", expiredKeys.Count);
		}
	}

	/// <summary>
	/// Returns active sessions.
	/// </summary>
	public IEnumerable<SessionRecord> GetActiveSessions()
	{
		return _sessions.Values.Where(s => s.DisconnectedAt == null).OrderByDescending(s => s.ConnectedAt);
	}

	/// <summary>
	/// Returns all tracked sessions.
	/// </summary>
	public IEnumerable<SessionRecord> GetAllSessions()
	{
		return _sessions.Values.OrderByDescending(s => s.DisconnectedAt == null).ThenByDescending(s => s.ConnectedAt);
	}
}
