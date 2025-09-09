using TwinCAT.Ads;

namespace HMEye.Twincat.Plc.EventLogService
{
	/// <summary>
	/// Provides thread-safe access to TwinCAT EventLogger using TwinCAT 3 EventLogger User mode API.
	/// AmsNetId, timeout, and reconnect delay are sourced from the same appsettings.json as used by PlcService.
	/// Alarms and events are converted to EventLoggerEvent type for easier use by .razor components.
	/// </summary>
	/// <remarks>
	/// Service should be registered in Program.cs as a singleton and hosted service using the IEventLoggerService interface:
	/// <code>
	/// builder.Services.AddSingleton<IEventLoggerService, EventLoggerService>();
	/// builder.Services.AddHostedService<IEventLoggerService>(sp => sp.GetRequiredService<IEventLoggerService>());
	/// </code>
	/// </remarks>
	public interface IEventLogService : IDisposable, IHostedService
	{
		/// <summary>
		/// Gets the first nMaxEvents from PLC EventLogger.
		/// </summary>
		/// <param name="nMaxEvents">Number of events to fetch. Defaults to 50.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task that resolves to a list of EventLogger events converted to <see cref="EventLogEvent"/> type.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task<List<EventLogEvent>> GetEventLogsAsync(uint nMaxEvents = 50, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all active EventLogger alarms.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task that resolves to a list of active EventLogger alarms converted to <see cref="EventLogEvent"/> type.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task<List<EventLogEvent>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Confirms a specific active alarm using a combination of eventId and eventClass.
		/// </summary>
		/// <param name="eventClass">The event class of the alarm to confirm.</param>
		/// <param name="eventId">The event ID of the alarm to confirm.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous confirmation operation.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="InvalidOperationException">Thrown when no alarm with the specified eventId and eventClass is found.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task ConfirmActiveAlarm(Guid eventClass, uint eventId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes all EventLogger events stored by PLC.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous clear operation.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task ClearEventLogs(CancellationToken cancellationToken = default);

		/// <summary>
		/// Confirms all active alarms.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous confirmation operation.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task ConfirmAllAlarms(CancellationToken cancellationToken = default);

		/// <summary>
		/// Clears all active alarms. Note that the PLC may require confirmation before clearing.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous clear operation.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task ClearAllAlarms(CancellationToken cancellationToken = default);
	}
}