using HMEye.Twincat.Plc.EventLogService;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;
using TwinCAT.Ads;

namespace HMEye.Twincat.Cache.EventLogCache
{
	/// <summary>
	/// Caches PLC events and alarms using immutable collections for thread-safe, read-heavy access.
	/// </summary>
	public class EventLogCacheService : IEventLogCacheService, IHostedService
	{
		private readonly IEventLogService _eventLogger;
		private readonly ILogger<EventLogCacheService> _logger;
		private readonly EventLogCacheSettings _settings;
		private readonly CancellationTokenSource _cts = new();
		private readonly object _lock = new();
		private Timer? _alarmRefreshTimer;
		private Timer? _eventRefreshTimer;
		private ImmutableList<EventLogEvent> _eventsCache = ImmutableList<EventLogEvent>.Empty;
		private ImmutableList<EventLogEvent> _activeAlarmsCache = ImmutableList<EventLogEvent>.Empty;
		private bool _error;
		private string _errorMessage = "";
		private bool _disposed;

		public event EventHandler? AlarmsCacheUpdated;
		public event EventHandler? EventsCacheUpdated;

		public bool Error => _error;
		public string ErrorMessage => _errorMessage;

		public EventLogCacheService(
			IEventLogService eventLogger,
			ILogger<EventLogCacheService> logger,
			IOptions<EventLogCacheSettings> options)
		{
			_eventLogger = eventLogger ?? throw new ArgumentNullException(nameof(eventLogger));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

			// Validate settings
			if (_settings.AlarmRefreshIntervalSeconds <= 0)
				throw new ArgumentException("RefreshIntervalSeconds mustS be positive.", nameof(options));
			if (_settings.EventRefreshIntervalSeconds <= 0)
				throw new ArgumentException("EventRefreshIntervalSeconds must be positive.", nameof(options));
			if (_settings.MaxCachedEvents == 0)
				throw new ArgumentException("MaxCachedEvents must be positive.", nameof(options));
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Start initial refresh for both caches
			await Task.WhenAll(
				RefreshAlarmsCacheAsync(cancellationToken),
				RefreshEventsCacheAsync(cancellationToken)
			);
			SchedulePeriodicRefreshes();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_cts.Cancel();
			_alarmRefreshTimer?.Dispose();
			_eventRefreshTimer?.Dispose();
			_cts.Dispose();
			return Task.CompletedTask;
		}

		private void SchedulePeriodicRefreshes()
		{
			_alarmRefreshTimer = new Timer(
				async _ =>
				{
					try
					{
						await RefreshAlarmsCacheAsync(_cts.Token);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Background alarm cache refresh failed");
					}
				},
				null,
				TimeSpan.FromSeconds(_settings.AlarmRefreshIntervalSeconds),
				TimeSpan.FromSeconds(_settings.AlarmRefreshIntervalSeconds));

			_eventRefreshTimer = new Timer(
				async _ =>
				{
					try
					{
						await RefreshEventsCacheAsync(_cts.Token);
					}
					catch (Exception ex)
					{
						_logger.LogError(ex, "Background event cache refresh failed");
					}
				},
				null,
				TimeSpan.FromSeconds(_settings.EventRefreshIntervalSeconds),
				TimeSpan.FromSeconds(_settings.EventRefreshIntervalSeconds));
		}

		public IReadOnlyCollection<EventLogEvent> GetCachedEvents() => _eventsCache;

		public IReadOnlyCollection<EventLogEvent> GetCachedActiveAlarms() => _activeAlarmsCache;

		private async Task RefreshAlarmsCacheAsync(CancellationToken cancellationToken)
		{
			const int maxRetries = 3;
			int retryCount = 0;

			while (retryCount < maxRetries)
			{
				try
				{
					var alarms = await _eventLogger.GetActiveAlarmsAsync(cancellationToken);
					Interlocked.Exchange(ref _activeAlarmsCache, alarms.ToImmutableList());
					ClearError();
					AlarmsCacheUpdated?.Invoke(this, EventArgs.Empty);
					_logger.LogDebug("PLC alarm cache refreshed - Alarms: {AlarmCount}", alarms.Count);
					return;
				}
				catch (AdsErrorException ex)
				{
					retryCount++;
					if (retryCount == maxRetries)
					{
						SetError($"Failed to refresh PLC alarm cache after {maxRetries} attempts: {ex.Message}");
						return;
					}
					_logger.LogWarning(ex, "ADS error during alarm cache refresh, retrying ({RetryCount}/{MaxRetries})", retryCount, maxRetries);
					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					SetError($"Error refreshing PLC alarm cache: {ex.Message}");
					return;
				}
			}
		}

		private async Task RefreshEventsCacheAsync(CancellationToken cancellationToken)
		{
			const int maxRetries = 3;
			int retryCount = 0;

			while (retryCount < maxRetries)
			{
				try
				{
					var events = await _eventLogger.GetEventLogsAsync(_settings.MaxCachedEvents, cancellationToken);
					Interlocked.Exchange(ref _eventsCache, events.ToImmutableList());
					ClearError();
					EventsCacheUpdated?.Invoke(this, EventArgs.Empty);
					_logger.LogDebug("PLC event cache refreshed - Events: {EventCount}", events.Count);
					return;
				}
				catch (AdsErrorException ex)
				{
					retryCount++;
					if (retryCount == maxRetries)
					{
						SetError($"Failed to refresh PLC event cache after {maxRetries} attempts: {ex.Message}");
						return;
					}
					_logger.LogWarning(ex, "ADS error during event cache refresh, retrying ({RetryCount}/{MaxRetries})", retryCount, maxRetries);
					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					SetError($"Error refreshing PLC event cache: {ex.Message}");
					return;
				}
			}
		}

		private void SetError(string message)
		{
			lock (_lock)
			{
				_errorMessage = message;
				_error = true;
			}
			_logger.LogError(message);
		}

		private void ClearError()
		{
			lock (_lock)
			{
				_errorMessage = "";
				_error = false;
			}
		}

		public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
		{
			await Task.WhenAll(
				RefreshAlarmsCacheAsync(cancellationToken),
				RefreshEventsCacheAsync(cancellationToken)
			);
		}

		public async Task ConfirmActiveAlarm(Guid eventClass, uint eventId, CancellationToken cancellationToken = default)
		{
			try
			{
				await _eventLogger.ConfirmActiveAlarm(eventClass, eventId, cancellationToken);
				ClearError();
			}
			catch (Exception ex)
			{
				SetError($"Error confirming active alarm: {ex.Message}");
			}
			await RefreshAlarmsCacheAsync(cancellationToken);
		}

		public async Task ConfirmAllAlarms(CancellationToken cancellationToken = default)
		{
			try
			{
				await _eventLogger.ConfirmAllAlarms(cancellationToken);
				ClearError();
			}
			catch (Exception ex)
			{
				SetError($"Error confirming all alarms: {ex.Message}");
			}
			await RefreshAlarmsCacheAsync(cancellationToken);
		}

		public async Task ClearEventLogs(CancellationToken cancellationToken = default)
		{
			try
			{
				await _eventLogger.ClearEventLogs(cancellationToken);
				ClearError();
			}
			catch (Exception ex)
			{
				SetError($"Error clearing event logs: {ex.Message}");
			}
			await RefreshEventsCacheAsync(cancellationToken);
		}

		public async Task ClearAllAlarms(CancellationToken cancellationToken = default)
		{
			try
			{
				await _eventLogger.ClearAllAlarms(cancellationToken);
				ClearError();
			}
			catch (Exception ex)
			{
				SetError($"Error clearing all alarms: {ex.Message}");
			}
			await RefreshAlarmsCacheAsync(cancellationToken);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_cts.Cancel();
			_alarmRefreshTimer?.Dispose();
			_eventRefreshTimer?.Dispose();
			_cts.Dispose();
			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}
}