using HMEye.Twincat.Contracts.Models;
using Microsoft.Extensions.Options;
using System.Globalization;
using TcEventLoggerAdsProxyLib;
using TwinCAT.Ads;

namespace HMEye.Twincat.Plc.EventLogService
{
	public class EventLogService : IDisposable, IHostedService, IEventLogService
	{
		private readonly ILogger<EventLogService> _logger;
		private readonly SemaphoreSlim _lock = new(1, 1);
		private readonly TcEventLogger _eventLogger = new();
		private readonly TimeSpan _defaultTimeout;
		private readonly TimeSpan _reconnectDelay;
		private readonly string? _netId;
		private volatile bool _disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="EventLogService"/> class.
		/// </summary>
		/// <param name="logger">The logger instance used for logging service events and errors.</param>
		/// <param name="options">The configuration options containing TwinCAT settings such as NetId, timeout, and reconnect delay.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when TwinCAT settings are not configured in <paramref name="options"/>.</exception>
		/// <exception cref="Exception">Thrown when configuration or connection setup fails.</exception>
		public EventLogService(ILogger<EventLogService> logger, IOptions<TwincatSettings> options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			try
			{
				// Get netId, timeout, and reconnect delay from appsettings.json
				var settings = options.Value ?? throw new InvalidOperationException("TwincatSettings is not configured.");
				_netId = settings.NetId;
				_defaultTimeout = TimeSpan.FromSeconds(settings.Timeout);
				_reconnectDelay = TimeSpan.FromSeconds(settings.ReconnectDelaySeconds);
			}
			catch (Exception ex)
			{
				_logger.LogError(
					ex,
					"Failed to initialize TwincatEventLoggerService due to an error in configuration or connection setup."
				);
				throw;
			}
		}

		/// <summary>
		/// Creates a linked cancellation token source that combines the external token with a timeout token.
		/// </summary>
		/// <param name="externalToken">The external cancellation token provided by the caller.</param>
		/// <returns>A tuple containing the linked cancellation token source and the timeout cancellation token source.</returns>
		private (CancellationTokenSource linkedCts, CancellationTokenSource timeoutCts) CreateLinkedCancellationTokenSource(CancellationToken externalToken)
		{
			var timeoutCts = new CancellationTokenSource(_defaultTimeout);
			var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, timeoutCts.Token);
			return (linkedCts, timeoutCts);
		}

		/// <summary>
		/// Checks connection status of EventLogger.
		/// </summary>
		/// <returns>True if the EventLogger is connected or if a reconnect attempt is successful; otherwise, false.</returns>
		private bool EnsureConnected()
		{
			if (!_eventLogger.IsConnected && _netId != null)
			{
				try
				{
					_logger.LogInformation("Attempting to connect to TwinCAT Event Logger.");
					_eventLogger.Connect(_netId);
					_logger.LogInformation("Successfully connected to TwinCAT Event Logger.");
					return true;
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while connecting to TwinCAT Event Logger.");
					return false;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to connect to TwinCAT Event Logger.");
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Handles the connection state change event for the EventLogger.
		/// </summary>
		/// <param name="reason">The reason for the connection state change.</param>
		private async void OnConnectionStateChanged(ConnectionChangeReasonEnum reason)
		{
			try
			{
				if (reason == ConnectionChangeReasonEnum.CCR_ConnectionLost)
				{
					_logger.LogWarning("Connection to TwinCAT Event Logger lost. Attempting to reconnect...");
					await Task.Delay(_reconnectDelay);
					EnsureConnected();
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning("Error occurred in handling Event Logger ConnectionStateChanged event: {Message}", ex.Message);
			}
		}

		public Task StartAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				_eventLogger.ConnectionStateChanged += OnConnectionStateChanged;
				EnsureConnected();
				_logger.LogInformation(
					"Initialized TwincatEventLoggerService with NetId: {NetId}, Timeout: {Timeout}s, ReconnectDelay: {ReconnectDelay}s",
					_netId,
					_defaultTimeout.TotalSeconds,
					_reconnectDelay.TotalSeconds
				);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to TwincatEventLoggerService");
				throw;
			}
		}

		public Task StopAsync(CancellationToken cancellationToken = default)
		{
			Dispose();
			return Task.CompletedTask;
		}

		public async Task<List<EventLogEvent>> GetEventLogsAsync(
			uint nMaxEvents = 50,
			CancellationToken cancellationToken = default
		)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					ITcLoggedEventCollection events = await Task.Run(() => _eventLogger.GetLoggedEvents(nMaxEvents), linkedCts.Token);
					return ConvertToLocalEvents(events);
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while retrieving events.");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while retrieving events.");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogWarning(canceledEx, "Operation was canceled while retrieving events.");
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "An unexpected error occurred while retrieving events.");
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		/// <summary>
		/// Converts a collection of logged events to a list of <see cref="EventLogEvent"/> objects.
		/// </summary>
		/// <param name="eventCollection">The collection of logged events to convert.</param>
		/// <returns>A list of converted <see cref="EventLogEvent"/> objects.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="eventCollection"/> is null.</exception>
		private static List<EventLogEvent> ConvertToLocalEvents(ITcLoggedEventCollection eventCollection)
		{
			ArgumentNullException.ThrowIfNull(eventCollection);

			return eventCollection
				.OfType<TcLoggedEvent>()
				.Select(x => new EventLogEvent
				{
					EventClass = x.EventClass,
					EventId = x.EventId,
					EventText = x.GetText(CultureInfo.CurrentCulture.LCID),
					FileTimeRaised = x.FileTimeRaised,
					FileTimeCleared = x.FileTimeCleared,
					FileTimeConfirmed = x.FileTimeConfirmed,
					IsActive = x.IsActive,
					IsRaised = x.FileTimeRaised != 0,
					IsConfirmed = x.FileTimeConfirmed != 0,
					IsCleared = x.FileTimeCleared != 0,
					JsonAttribute = x.JsonAttribute,
					SeverityLevel = x.SeverityLevel,
					SourceName = x.SourceName,
					TimeCleared = x.TimeCleared.ToLocalTime(),
					TimeConfirmed = x.TimeConfirmed.ToLocalTime(),
					TimeRaised = x.TimeRaised.ToLocalTime(),
					WithConfirmation = x.WithConfirmation,
				})
				.ToList();
		}

		/// <summary>
		/// Converts a collection of alarms to a list of <see cref="EventLogEvent"/> objects.
		/// </summary>
		/// <param name="alarmCollection">The collection of alarms to convert.</param>
		/// <returns>A list of converted <see cref="EventLogEvent"/> objects.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="alarmCollection"/> is null.</exception>
		private static List<EventLogEvent> ConvertToLocalEvents(ITcAlarmCollection alarmCollection)
		{
			ArgumentNullException.ThrowIfNull(alarmCollection);

			return alarmCollection
				.OfType<TcAlarm>()
				.Select(x => new EventLogEvent
				{
					EventClass = x.EventClass,
					EventId = x.EventId,
					EventText = x.GetText(CultureInfo.CurrentCulture.LCID),
					FileTimeRaised = x.FileTimeRaised,
					FileTimeCleared = x.FileTimeCleared,
					FileTimeConfirmed = x.FileTimeConfirmed,
					IsActive = x.FileTimeRaised != 0 && x.FileTimeCleared == 0,
					IsRaised = x.FileTimeRaised != 0,
					IsConfirmed = x.FileTimeConfirmed != 0,
					IsCleared = x.FileTimeCleared != 0,
					JsonAttribute = x.JsonAttribute,
					SeverityLevel = x.SeverityLevel,
					SourceName = x.SourceName,
					TimeCleared = x.TimeCleared.ToLocalTime(),
					TimeConfirmed = x.TimeConfirmed.ToLocalTime(),
					TimeRaised = x.TimeRaised.ToLocalTime(),
				})
				.ToList();
		}

		public async Task<List<EventLogEvent>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					var activeAlarms = await Task.Run(() => _eventLogger.ActiveAlarms, linkedCts.Token);
					return ConvertToLocalEvents(activeAlarms);
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while retrieving active alarms.");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while retrieving active alarms.");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogWarning(canceledEx, "Operation was canceled while retrieving active alarms.");
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to retrieve active alarms.");
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task ConfirmActiveAlarm(Guid eventClass, uint eventId, CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					var alarm = await Task.Run(() => _eventLogger.ActiveAlarms.First(x => x.EventId == eventId && x.EventClass == eventClass), linkedCts.Token);
					await Task.Run(() => alarm.Confirm(), linkedCts.Token);
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while confirming active alarm for EventId: {EventId}, EventClass: {EventClass}", eventId, eventClass);
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while confirming active alarm for EventId: {EventId}, EventClass: {EventClass}", eventId, eventClass);
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogWarning(canceledEx, "Operation was canceled while confirming active alarm for EventId: {EventId}, EventClass: {EventClass}", eventId, eventClass);
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to confirm active alarm for EventId: {EventId}, EventClass: {EventClass}", eventId, eventClass);
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task ClearEventLogs(CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					await Task.Run(() => _eventLogger.ClearLoggedEvents(), linkedCts.Token);
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while clearing logged events.");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while clearing logged events.");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogWarning(canceledEx, "Operation was canceled while clearing logged events.");
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to clear event logs.");
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task ConfirmAllAlarms(CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					await Task.Run(() => _eventLogger.ConfirmAllAlarms(), linkedCts.Token);
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while confirming all alarms.");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while confirming all alarms.");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogWarning(canceledEx, "Operation was canceled while confirming all alarms.");
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to confirm all alarms.");
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task ClearAllAlarms(CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					await Task.Run(() => _eventLogger.ClearAllAlarms(), linkedCts.Token);
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while clearing all alarms.");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while clearing all alarms.");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogWarning(canceledEx, "Operation was canceled while clearing all alarms.");
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to clear all alarms.");
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_eventLogger.ConnectionStateChanged -= OnConnectionStateChanged;
			_eventLogger?.Dispose();
			_lock?.Dispose();

			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}
}