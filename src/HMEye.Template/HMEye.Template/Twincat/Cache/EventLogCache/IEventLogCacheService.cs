using HMEye.Twincat.Plc.EventLogService;

namespace HMEye.Twincat.Cache.EventLogCache
{
	/// <summary>
	/// Provides cached access to PLC events and alarms, optimized for read-heavy scenarios with thread-safe, lock-free reads.
	/// </summary>
	public interface IEventLogCacheService : IDisposable, IHostedService
	{
		/// <summary>
		/// Gets all cached events from the PLC event log, preserving order (e.g., by TimeRaised).
		/// </summary>
		/// <returns>A read-only collection of cached events.</returns>
		IReadOnlyCollection<EventLogEvent> GetCachedEvents();

		/// <summary>
		/// Gets all cached active alarms from the PLC, preserving order (e.g., by TimeRaised).
		/// </summary>
		/// <returns>A read-only collection of cached active alarms.</returns>
		IReadOnlyCollection<EventLogEvent> GetCachedActiveAlarms();

		/// <summary>
		/// Refreshes both event and alarm caches by polling the PLC.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous refresh operation.</returns>
		Task RefreshCacheAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Confirms an active alarm on the PLC and refreshes the alarm cache.
		/// </summary>
		/// <param name="eventClass">The GUID of the alarm's event class.</param>
		/// <param name="eventId">The ID of the alarm.</param>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ConfirmActiveAlarm(Guid eventClass, uint eventId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Confirms all active alarms on the PLC and refreshes the alarm cache.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ConfirmAllAlarms(CancellationToken cancellationToken = default);

		/// <summary>
		/// Clears all event logs on the PLC and refreshes the event cache.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ClearEventLogs(CancellationToken cancellationToken = default);

		/// <summary>
		/// Clears all alarms on the PLC and refreshes the alarm cache.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		Task ClearAllAlarms(CancellationToken cancellationToken = default);

		/// <summary>
		/// Indicates whether the service has encountered an error during PLC communication.
		/// </summary>
		bool Error { get; }

		/// <summary>
		/// Provides the error message if an error occurred during PLC communication.
		/// </summary>
		string ErrorMessage { get; }

		/// <summary>
		/// Event raised when the alarm cache is updated after a refresh or client action.
		/// </summary>
		event EventHandler AlarmsCacheUpdated;

		/// <summary>
		/// Event raised when the event cache is updated after a refresh or client action.
		/// </summary>
		event EventHandler EventsCacheUpdated;
	}
}