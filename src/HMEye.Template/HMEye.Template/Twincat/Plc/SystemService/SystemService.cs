using HMEye.Twincat.Contracts.Models;
using Microsoft.Extensions.Options;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.SystemService;

namespace HMEye.Twincat.Plc.SystemService
{
	public class SystemService : IDisposable, IHostedService, ISystemService
	{
		private readonly ILogger<SystemService> _logger;
		private readonly TimeSpan _defaultTimeout;
		private readonly TimeSpan _reconnectDelay;
		private readonly string? _netId;
		private readonly int _port;
		private readonly AdsClient _adsClientSystem = new();
		private readonly SemaphoreSlim _lock = new(1, 1);
		private bool _disposed;

		public event EventHandler? ConnectionSuccess;

		public event EventHandler? ConnectionLost;

		public bool IsConnected => _adsClientSystem.IsConnected;

		/// <summary>
		/// Initializes a new instance of the <see cref="SystemService"/> class.
		/// </summary>
		/// <param name="logger">The logger instance used for logging service events and errors.</param>
		/// <param name="options">The configuration options containing TwinCAT settings such as NetId and system port.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown when TwinCAT settings are not configured in <paramref name="options"/>.</exception>
		public SystemService(ILogger<SystemService> logger, IOptions<TwincatSettings> options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			var settings =
				options.Value
				?? throw new InvalidOperationException("TwincatSettings is not configured.");
			_netId = settings.NetId;
			_port = settings.SystemPort;
			_defaultTimeout = TimeSpan.FromSeconds(settings.Timeout);
			_reconnectDelay = TimeSpan.FromSeconds(settings.ReconnectDelaySeconds);
		}

		/// <summary>
		/// Creates a linked cancellation token source that combines the external token with a timeout token.
		/// </summary>
		/// <param name="externalToken">The external cancellation token provided by the caller.</param>
		/// <returns>A tuple containing the linked cancellation token source and the timeout cancellation token source.</returns>
		private (
			CancellationTokenSource linkedCts,
			CancellationTokenSource timeoutCts
		) CreateLinkedCancellationTokenSource(CancellationToken externalToken)
		{
			var timeoutCts = new CancellationTokenSource(_defaultTimeout);
			var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
				externalToken,
				timeoutCts.Token
			);
			return (linkedCts, timeoutCts);
		}

		/// <summary>
		/// Ensures that the system service connection is established. If not connected, attempts to reconnect.
		/// </summary>
		/// <returns>True if connected or reconnection was successful; otherwise, false.</returns>
		private bool EnsureConnected()
		{
			if (!_adsClientSystem.IsConnected && _netId != null)
			{
				try
				{
					_logger.LogInformation("Attempting to connect TwinCAT System AdsClient.");
					_adsClientSystem.Connect(_netId, _port);
					_logger.LogInformation("Successfully connected TwinCAT System AdsClient.");
					OnConnectionSuccess();
					return true;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to connect to TwinCAT System AdsClient.");
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Handles the connection state change event for the system AdsClient.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event data containing connection state change details.</param>
		private async void ConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
		{
			try
			{
				if (e.Reason == ConnectionStateChangedReason.Lost)
				{
					_logger.LogWarning(
						"System AdsClient connection lost. Attempting to reconnect..."
					);
					OnConnectionLost();
					await Task.Delay(_reconnectDelay);
					EnsureConnected();
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning("Error in System ConnectionStateChanged: {Message}", ex.Message);
			}
		}

		/// <summary>
		/// Raises the <see cref="ConnectionSuccess"/> event when the connection is established.
		/// </summary>
		private void OnConnectionSuccess()
		{
			ConnectionSuccess?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Raises the <see cref="ConnectionLost"/> event when the connection is lost.
		/// </summary>
		private void OnConnectionLost()
		{
			ConnectionLost?.Invoke(this, EventArgs.Empty);
		}

		public Task StartAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				_adsClientSystem.ConnectionStateChanged += ConnectionStateChanged;
				EnsureConnected();
				_logger.LogInformation(
					"Initialized SystemService with NetId: {NetId}, Port: {Port}",
					_netId,
					_port
				);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to initialize SystemService");
				throw;
			}
		}

		public Task StopAsync(CancellationToken cancellationToken = default)
		{
			_adsClientSystem.ConnectionStateChanged -= ConnectionStateChanged;
			Dispose();
			return Task.CompletedTask;
		}

		public async Task<AdsSysServState> ReadSystemStateAsync(
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
					var sysState = await _adsClientSystem.ReadSysServStateAsync(linkedCts.Token);
					return sysState.Value;
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while reading system state");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while reading system state");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogError(
						canceledEx,
						"Operation was canceled while reading system state"
					);
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to read system state");
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
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes of the service and releases resources.
		/// </summary>
		/// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				_adsClientSystem.ConnectionStateChanged -= ConnectionStateChanged;
				_adsClientSystem?.Dispose();
				_lock?.Dispose();
			}

			_disposed = true;
		}
	}
}