using System.Text;
using HMEye.Twincat.Contracts.Models;
using Microsoft.Extensions.Options;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace HMEye.Twincat.Plc.PlcService
{
	public class PlcService : IDisposable, IHostedService, IPlcService
	{
		private readonly ILogger<PlcService> _logger;
		private readonly TimeSpan _defaultTimeout;
		private readonly TimeSpan _reconnectDelay;
		private readonly string? _netId;
		private readonly int _port;
		private readonly AdsClient _adsClient = new();
		private readonly SemaphoreSlim _lock = new(1, 1);
		private ISymbolCollection<ISymbol>? _symbols;
		private IAdsSymbolLoader? _symbolLoader;
		private bool _disposed;

		public event EventHandler? ConnectionSuccess;

		public event EventHandler? ConnectionLost;

		public bool IsConnected => _adsClient.IsConnected;

		public PlcService(ILogger<PlcService> logger, IOptions<TwincatSettings> options)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			var settings = options.Value ?? throw new InvalidOperationException("TwincatSettings is not configured.");
			_netId = settings.NetId;
			_port = settings.PlcPort;
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
			var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, timeoutCts.Token);
			return (linkedCts, timeoutCts);
		}

		/// <summary>
		/// Ensures that the PLC connection is established. If not connected, attempts to reconnect.
		/// </summary>
		/// <returns>A task that resolves to true if connected or reconnection was successful; otherwise, false.</returns>
		private bool EnsureConnected()
		{
			if (!_adsClient.IsConnected && _netId != null)
			{
				try
				{
					_logger.LogInformation("Attempting to connect TwinCAT PLC AdsClient.");
					_adsClient.Connect(_netId, _port);
					_logger.LogInformation("Successfully connected TwinCAT PLC AdsClient.");
					OnConnectionSuccess();
					return true;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to connect to TwinCAT PLC AdsClient.");
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Ensures that the symbol loader is initialized for accessing PLC variables.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous initialization of the symbol loader.</returns>
		/// <exception cref="Exception">Thrown when symbol initialization fails.</exception>
		private async Task EnsureSymbolLoaderInitializedAsync(CancellationToken cancellationToken = default)
		{
			if (_symbolLoader != null)
				return;

			var settings = new SymbolLoaderSettings(SymbolsLoadMode.DynamicTree);
			_symbolLoader = (IAdsSymbolLoader)SymbolLoaderFactory.Create(_adsClient, settings);
			_symbols =
				(await _symbolLoader.GetSymbolsAsync(cancellationToken)).Symbols
				?? throw new Exception("Failed to initialize symbols.");
		}

		/// <summary>
		/// Handles the connection state change event for the PLC AdsClient.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The event data containing connection state change details.</param>
		private async void ConnectionStateChanged(object? sender, ConnectionStateChangedEventArgs e)
		{
			try
			{
				if (e.Reason == ConnectionStateChangedReason.Lost)
				{
					_logger.LogWarning("PLC AdsClient connection lost. Attempting to reconnect...");
					OnConnectionLost();
					await Task.Delay(_reconnectDelay);
					EnsureConnected();
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning("Error in PLC ConnectionStateChanged: {Message}", ex.Message);
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

		/// <summary>
		/// Starts the service and ensures the PLC connection is established.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the start operation.</param>
		/// <returns>A task representing the asynchronous start operation.</returns>
		public Task StartAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				_adsClient.ConnectionStateChanged += ConnectionStateChanged;
				EnsureConnected();
				_logger.LogInformation(
					"Initialized PLC comms with NetId: {NetId}, Port: {Port}, DefaultTimeout: {DefaultTimeout}, ReconnectDelay, {ReconnectDelay}",
					_netId,
					_port,
					_defaultTimeout.TotalSeconds,
					_reconnectDelay.TotalSeconds
				);
				return Task.CompletedTask;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to start TwincatPlcService");
				throw;
			}
		}

		/// <summary>
		/// Stops the service and disposes of resources.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the stop operation.</param>
		/// <returns>A task representing the asynchronous stop operation.</returns>
		public Task StopAsync(CancellationToken cancellationToken = default)
		{
			_adsClient.ConnectionStateChanged -= ConnectionStateChanged;
			Dispose();
			return Task.CompletedTask;
		}

		public async Task<ResultReadDeviceState> ReadDeviceStateAsync(CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					var state = await _adsClient.ReadStateAsync(linkedCts.Token);
					return state;
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while reading device state");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while reading device state");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogError(canceledEx, "Operation was canceled while reading device state");
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to read device state");
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task<ResultDeviceInfo> ReadDeviceInfoAsync(CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					var info = await _adsClient.ReadDeviceInfoAsync(linkedCts.Token);
					return info;
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while reading device info");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while reading device info");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogError(canceledEx, "Operation was canceled while reading device info");
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to read device info");
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task PlcCommandAsync(AdsState adsState, CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					var state = await _adsClient.ReadStateAsync(linkedCts.Token);
					ushort deviceState = (ushort)state.State.DeviceState;
					await _adsClient.WriteControlAsync(adsState, deviceState, linkedCts.Token);
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while issuing PLC command");
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(timeoutEx, "Timeout occurred while issuing PLC command");
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogError(canceledEx, "Operation was canceled while issuing PLC command");
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to issue PLC command");
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task<T> ReadAsync<T>(string variableName, CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					var resultRead = await _adsClient.ReadValueAsync<T>(variableName, linkedCts.Token);
					if (!resultRead.Succeeded || resultRead.Value == null)
					{
						_logger.LogError("Failed to read value for symbol: {VariableName}", variableName);
						throw new InvalidOperationException(
							$"Failed to read value for symbol: {variableName}"
						);
					}
					return resultRead.Value;
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while reading symbol: '{variableName}'", variableName);
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(
						timeoutEx,
						"Timeout occurred while reading symbol: '{variableName}'",
						variableName
					);
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogError(
						canceledEx,
						"Operation was canceled while reading symbol: '{variableName}'",
						variableName
					);
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to read symbol: '{variableName}'", variableName);
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task WriteAsync<T>(string variableName, T value, CancellationToken cancellationToken = default) where T : notnull
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					if (value is null)
					{
						_logger.LogError("Value for symbol '{variableName}' is null.", variableName);
						throw new ArgumentNullException(
							nameof(value),
							$"Value for symbol '{variableName}' cannot be null."
						);
					}
					if (value is null) throw new ArgumentException(nameof(value));
					var resultWrite = await _adsClient.WriteValueAsync(variableName, value, linkedCts.Token);
					if (!resultWrite.Succeeded)
					{
						_logger.LogError("Failed to write value for symbol: {VariableName}", variableName);
						throw new InvalidOperationException(
							$"Failed to write value for symbol: {variableName}"
						);
					}
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while writing symbol: '{variableName}'", variableName);
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(
						timeoutEx,
						"Timeout occurred while writing symbol: '{variableName}'",
						variableName
					);
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogError(
						canceledEx,
						"Operation was canceled while writing symbol: '{variableName}'",
						variableName
					);
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to write symbol: '{variableName}'", variableName);
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task<T> ReadDynamicAsync<T>(string variableName, CancellationToken cancellationToken = default)
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					await EnsureSymbolLoaderInitializedAsync(linkedCts.Token);
					if (_symbols == null)
						throw new Exception("Symbols not initialized.");

					if (_symbols[variableName] is not DynamicSymbol symbol)
					{
						_logger.LogError("Symbol '{variableName}' not found or not a DynamicSymbol.", variableName);
						throw new KeyNotFoundException($"Symbol '{variableName}' not found.");
					}

					var resultRead = await symbol.ReadValueAsync(linkedCts.Token);
					if (!resultRead.Succeeded || resultRead.Value == null)
					{
						_logger.LogError("Failed to read dynamic value for symbol: {VariableName}", variableName);
						throw new InvalidOperationException(
							$"Failed to read dynamic value for symbol: {variableName}"
						);
					}

					return (T)resultRead.Value;
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while reading symbol: '{variableName}'", variableName);
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(
						timeoutEx,
						"Timeout occurred while reading symbol: '{variableName}'",
						variableName
					);
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogError(
						canceledEx,
						"Operation was canceled while reading symbol: '{variableName}'",
						variableName
					);
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to read symbol: '{variableName}'", variableName);
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		public async Task WriteDynamicAsync<T>(
			string variableName,
			T value,
			CancellationToken cancellationToken = default
		) where T : notnull
		{
			var (linkedCts, timeoutCts) = CreateLinkedCancellationTokenSource(cancellationToken);
			using (linkedCts)
			using (timeoutCts)
			{
				await _lock.WaitAsync(linkedCts.Token);
				try
				{
					await EnsureSymbolLoaderInitializedAsync(linkedCts.Token);
					if (_symbols == null)
						throw new Exception("Symbols not initialized.");

					if (_symbols[variableName] is not DynamicSymbol symbol)
					{
						_logger.LogError("Symbol '{variableName}' not found or not a DynamicSymbol.", variableName);
						throw new KeyNotFoundException($"Symbol '{variableName}' not found.");
					}

					if (value == null)
					{
						_logger.LogError("Value for symbol '{variableName}' is null.", variableName);
						throw new ArgumentNullException(
							nameof(value),
							$"Value for symbol '{variableName}' cannot be null."
						);
					}

					if (typeof(T) == typeof(string))
					{
						if (symbol.DataType == null)
						{
							throw new InvalidOperationException($"DataType is null for symbol '{variableName}'.");
						}
						int maxLength = symbol.DataType.Size;
						string stringValue = value.ToString() ?? string.Empty;
						var encoding = symbol.ValueEncoding == Encoding.UTF8 ? Encoding.UTF8 : Encoding.Latin1;
						var byteCount = encoding.GetByteCount(stringValue);

						if (byteCount >= maxLength)
						{
							_logger.LogError(
								"String variable '{variableName}' exceeds maximum allowed length.",
								variableName
							);
							throw new ArgumentException(
								$"String length exceeds maximum allowed length of {maxLength - 1} characters."
							);
						}
					}

					var resultWrite = await symbol.WriteValueAsync(value, linkedCts.Token);
					if (!resultWrite.Succeeded)
					{
						_logger.LogError("Failed to write dynamic value for variable: {VariableName}", variableName);
						throw new InvalidOperationException(
							$"Failed to write dynamic value for variable: {variableName}"
						);
					}
				}
				catch (AdsErrorException adsEx)
				{
					_logger.LogError(adsEx, "ADS error occurred while writing symbol: '{variableName}'", variableName);
					throw;
				}
				catch (TimeoutException timeoutEx)
				{
					_logger.LogError(
						timeoutEx,
						"Timeout occurred while writing symbol: '{variableName}'",
						variableName
					);
					throw;
				}
				catch (OperationCanceledException canceledEx)
				{
					_logger.LogError(
						canceledEx,
						"Operation was canceled while writing symbol: '{variableName}'",
						variableName
					);
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to write symbol: '{variableName}'", variableName);
					throw;
				}
				finally
				{
					_lock.Release();
				}
			}
		}

		/// <summary>
		/// Disposes of the service and releases all resources.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
				return;

			_adsClient.ConnectionStateChanged -= ConnectionStateChanged;
			_adsClient.Dispose();
			_lock.Dispose();

			_disposed = true;
		}
	}
}
