namespace HMEye.Twincat.Cache.EventLogCache;

public class EventLogCacheSettings
{
	public const string SectionName = "PlcEventCache";

	/// <summary>
	/// Cache refresh interval for active alarms in seconds (default: 2).
	/// </summary>
	public int AlarmRefreshIntervalSeconds { get; set; } = 2;

	/// <summary>
	/// Cache refresh interval for historical events in seconds (default: 5).
	/// </summary>
	public int EventRefreshIntervalSeconds { get; set; } = 5;

	/// <summary>
	/// Maximum number of events to cache (default: 100).
	/// </summary>
	public uint MaxCachedEvents { get; set; } = 100;
}