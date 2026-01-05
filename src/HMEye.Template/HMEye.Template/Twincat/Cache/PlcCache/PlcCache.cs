using HMEye.Twincat.Plc.PlcService;
using System.Collections.Concurrent;
using TwinCAT.Ads;

namespace HMEye.Twincat.Cache.PlcCache
{
	public class PlcCache : IPlcCache, IHostedService, IDisposable
	{
		private readonly IPlcService _plcService;
		private readonly ILogger<PlcCache> _logger;
		private readonly ConcurrentDictionary<string, IPlcCacheItem> _cacheItems;
		private readonly ConcurrentQueue<IPlcCacheWriteOperation> _writeQueue = new();
		private Timer? _pollingTimer;
		private readonly TimeSpan _basePollingInterval = TimeSpan.FromMilliseconds(100);
		private volatile bool _error;
		private string _errorMessage = string.Empty;
		private DateTime _lastErrorTime;
		private int _errorCount;
		private readonly int _maxErrorsBeforeDisable = 10;
		private readonly object _errorLock = new();

		public bool Error => _error;
		public string ErrorMessage => _errorMessage;
		public DateTime LastErrorTime => _lastErrorTime;

		public PlcCache(IPlcService plcService, ILogger<PlcCache> logger, IEnumerable<PlcCacheItemConfig>? configs)
		{
			_plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			_cacheItems = new ConcurrentDictionary<string, IPlcCacheItem>(
				(configs ?? Enumerable.Empty<PlcCacheItemConfig>()).ToDictionary(
					cfg => cfg.Address,
					cfg => CreateCacheItem(cfg, _plcService)
				)
			);
		}

		private static IPlcCacheItem CreateCacheItem(PlcCacheItemConfig config, IPlcService plcService)
		{
			if (config.IsDynamic)
			{
				var cacheItemType = typeof(PlcCacheItemDynamic<>).MakeGenericType(config.Type);
				return (IPlcCacheItem)
					Activator.CreateInstance(
						cacheItemType,
						config.Address,
						config.PollInterval,
						plcService,
						config.IsReadOnly
					)!;
			}
			else if (config.IsArray)
			{
				var cacheItemType = typeof(PlcCacheItem<>).MakeGenericType(config.Type);
				return (IPlcCacheItem)
					Activator.CreateInstance(
						cacheItemType,
						config.Address,
						config.PollInterval,
						plcService,
						config.IsReadOnly
					)!;
			}
			else
			{
				var cacheItemType = typeof(PlcCacheItem<>).MakeGenericType(config.Type);
				return (IPlcCacheItem)
					Activator.CreateInstance(
						cacheItemType,
						config.Address,
						config.PollInterval,
						plcService,
						config.IsReadOnly
					)!;
			}
		}

		private void PollPlcValues(object? state)
		{
			_ = PollPlcValuesAsync();
		}

		private async Task PollPlcValuesAsync()
		{
			try
			{
				if (_error && _errorCount >= _maxErrorsBeforeDisable)
					return;

				foreach (var item in _cacheItems.Values)
				{
					if (item.IsDueForPolling())
					{
						try
						{
							await item.GetAsync();
							ClearError();
						}
						catch (Exception ex) when (ex is not OperationCanceledException)
						{
							SetError($"Failed to poll variable: {item.Address}", ex);
						}
					}
				}

				for (int i = 0; i < 5 && _writeQueue.TryDequeue(out var operation); i++)
				{
					try
					{
						await operation.ExecuteAsync(_plcService, default);
						if (_cacheItems.TryGetValue(operation.Address, out var item))
						{
							var value = operation.Value;
							if (value != null)
								await item.SetAsync(value);
						}
						if (_writeQueue.Count < 50 && _error)
							ClearError();
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						SetError($"Failed to write value: {operation.Address}", ex);
						_writeQueue.Enqueue(operation);
					}
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				SetError("Polling cycle failed", ex);
			}
		}

		public async Task<PlcCacheReadResult<T>> TryReadImmediateAsync<T>(string address)
		{
			if (_cacheItems.TryGetValue(address, out var item) && item.Type == typeof(T))
			{
				try
				{
					await item.GetAsync();
					return new PlcCacheReadResult<T>((T)item.GetValue()!);
				}
				catch (Exception ex)
				{
					SetError($"Failed to read {address}", ex);
					return new PlcCacheReadResult<T>(default, true, ex.Message);
				}
			}
			return new PlcCacheReadResult<T>(default, true, $"Variable {address} not found or type mismatch");
		}

		public async Task<T?> ReadImmediateAsync<T>(string address)
		{
			var result = await TryReadImmediateAsync<T>(address);
			return result.Error ? default : result.Value;
		}

		public PlcCacheReadResult<T> TryReadCached<T>(string address)
		{
			if (_cacheItems.TryGetValue(address, out var item) && item.Type == typeof(T))
			{
				return new PlcCacheReadResult<T>((T)item.GetValue()!);
			}
			return new PlcCacheReadResult<T>(default, true, $"Variable {address} not found or type mismatch");
		}

		public T? ReadCached<T>(string address)
		{
			var result = TryReadCached<T>(address);
			return result.Error ? default : result.Value;
		}

		public PlcCacheWriteResult TryWriteQueue<T>(string address, T value)
			where T : notnull
		{
			if (_cacheItems.TryGetValue(address, out var item) && item.Type.IsAssignableFrom(typeof(T)))
			{
				if (item.IsReadOnly)
					return new PlcCacheWriteResult(false, $"Variable {address} is read-only");

				var operation = item.CreateWriteOperation(value);
				if (_writeQueue.Count < 100)
				{
					_writeQueue.Enqueue(operation);
					return new PlcCacheWriteResult(true);
				}
				else
				{
					var errorMsg = $"Write queue overflow for operation: {address}. Consider reducing write frequency.";
					SetError(errorMsg, new InvalidOperationException(errorMsg));
					_logger.LogWarning(errorMsg);
					return new PlcCacheWriteResult(false, errorMsg);
				}
			}
			return new PlcCacheWriteResult(false, $"Variable {address} not found or type mismatch");
		}

		public void WriteQueue<T>(string address, T value)
			where T : notnull
		{
			var result = TryWriteQueue(address, value);
		}

		public async Task<PlcCacheWriteResult> TryWriteImmediateAsync<T>(string address, T value)
			where T : notnull
		{
			if (_cacheItems.TryGetValue(address, out var item) && item.Type.IsAssignableFrom(typeof(T)))
			{
				if (item.IsReadOnly)
					return new PlcCacheWriteResult(false, $"Variable {address} is read-only");

				try
				{
					await item.SetAsync(value);
					return new PlcCacheWriteResult(true);
				}
				catch (Exception ex)
				{
					SetError($"Immediate write failed for {address}", ex);
					return new PlcCacheWriteResult(false, ex.Message);
				}
			}
			return new PlcCacheWriteResult(false, $"Variable {address} not found or type mismatch");
		}

		public async Task WriteImmediateAsync<T>(string address, T value)
			where T : notnull
		{
			var result = await TryWriteImmediateAsync(address, value);
		}

		public void AddCacheItem(PlcCacheItemConfig config)
		{
			if (config == null)
			{
				throw new ArgumentNullException(nameof(config), "Cache item configuration cannot be null.");
			}

			if (_cacheItems.ContainsKey(config.Address))
			{
				throw new InvalidOperationException($"Cache item with address {config.Address} already exists.");
			}

			try
			{
				var cacheItem = CreateCacheItem(config, _plcService);
				if (_cacheItems.TryAdd(config.Address, cacheItem))
				{
					_logger.LogInformation("Added cache item: {Address}", config.Address);
				}
				else
				{
					_logger.LogWarning("Failed to add cache item {Address} to the dictionary.", config.Address);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to add cache item: {Address}", config.Address);
				throw;
			}
		}

		public void RemoveCacheItem(string address)
		{
			if (string.IsNullOrEmpty(address))
			{
				throw new ArgumentException("Address cannot be null or empty.", nameof(address));
			}

			if (_cacheItems.TryRemove(address, out _))
			{
				_logger.LogInformation("Removed cache item: {Address}", address);
			}
			// Silently ignore if address doesn't exist
		}

		public void ResetError()
		{
			lock (_errorLock)
			{
				_error = false;
				_errorCount = 0;
				_errorMessage = string.Empty;
				_pollingTimer?.Change(_basePollingInterval, _basePollingInterval);
			}
			_logger.LogInformation("Error state reset");
		}

		private void SetError(string message, Exception ex)
		{
			lock (_errorLock)
			{
				_error = true;
				_errorCount++;
				_lastErrorTime = DateTime.UtcNow;
				_errorMessage = $"{message}: {ex.Message}";
				if (_errorCount >= _maxErrorsBeforeDisable)
					_pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
			}
			_logger.LogError(ex, message);
		}

		private void ClearError()
		{
			lock (_errorLock)
			{
				_error = false;
				_errorCount = 0;
				_errorMessage = string.Empty;
			}
		}

		private void OnConnectionLost(object? sender, EventArgs args)
		{
			SetError($"Ads connection lost", new AdsErrorException());
		}

		private void OnConnectionSuccess(object? sender, EventArgs args)
		{
			ResetError();
		}

		public async Task StartAsync(CancellationToken cts)
		{
			// validate cache items list
			foreach (var item in _cacheItems.Values)
			{
				try
				{
					await item.GetAsync();
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					throw new InvalidOperationException(
						$"Variable: {item.Address} failed initial symbol validation.",
						ex
					);
				}
			}

			_pollingTimer = new Timer(PollPlcValues, null, _basePollingInterval, _basePollingInterval);
			_plcService.ConnectionLost += OnConnectionLost;
			_plcService.ConnectionSuccess += OnConnectionSuccess;
		}

		public Task StopAsync(CancellationToken cts)
		{
			_pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite);
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_pollingTimer?.Dispose();
			_plcService.ConnectionLost -= OnConnectionLost;
			_plcService.ConnectionSuccess -= OnConnectionSuccess;
		}
	}
}
