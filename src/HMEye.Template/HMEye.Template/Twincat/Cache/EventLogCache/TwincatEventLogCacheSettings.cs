namespace HMEye.TwincatServices.Cache.EventLogCache;

public class TwincatEventLogCacheSettings
{
	public const string SectionName = "TwincatEventCache";

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
