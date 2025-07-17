using HMEye.TwincatServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HMEye.Services;

public class PlcDataCacheService : IPlcDataCacheService
{
	private readonly IPlcService _plcService;
	private readonly ILogger<PlcDataCacheService> _logger;
	private readonly ConcurrentDictionary<string, object> _cache = new();
	private readonly ConcurrentQueue<(IWriteRequest request, CancellationToken cts)> _writeQueue = new();
	private readonly Timer _pollingTimer;
	private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(300);
	private readonly List<IMonitoredVariable> _monitoredVariables = new()
	{
		new MonitoredVariable<short>("GVL.MachineSpeed"),
		new MonitoredVariable<double>("GVL.Temperature")
	};

	private volatile bool _error;
	private DateTime _lastErrorTime;
	private string _errorMessage = string.Empty;
	private readonly object _errorLock = new();

	public bool Error => _error;
	public string ErrorMessage => Volatile.Read(ref _errorMessage);
	public DateTime LastErrorTime => _lastErrorTime;

	public PlcDataCacheService(IPlcService plcService, ILogger<PlcDataCacheService> logger)
	{
		_plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_pollingTimer = new Timer(PollPlcValues, null, _pollingInterval, _pollingInterval);
	}

	private async void PollPlcValues(object? state)
	{
		try
		{
			if (_error) ClearError();

			foreach (var variable in _monitoredVariables)
			{
				try
				{
					await variable.PollAsync(_plcService, _cache);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					SetError($"Failed to read monitored value: {variable.Address}", ex);
				}
			}

			for (int i = 0; i < 5 && _writeQueue.TryDequeue(out var entry); i++)
			{
				var (request, cts) = entry;
				if (cts.IsCancellationRequested)
					continue;

				try
				{
					await request.ExecuteAsync(_plcService, cts);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					SetError($"Failed to write value: {request.Address}", ex);
					_writeQueue.Enqueue(entry);
					break;
				}
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			SetError("Polling cycle failed", ex);
		}
	}

	public async Task<ReadResult<T>> GetValueAsync<T>(string address, CancellationToken cts = default)
	{
		try
		{
			if (_cache.TryGetValue(address, out var cachedValue))
			{
				if (cachedValue is T typedValue)
					return new ReadResult<T>(typedValue);
				_logger.LogWarning("Type mismatch for {Address}: expected {Expected}, got {Actual}", address, typeof(T), cachedValue?.GetType());
			}

			var value = await _plcService.ReadVariableAsync<T>(address, cts);
			_cache[address] = value;
			return new ReadResult<T>(value);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			SetError($"Get failed for {address}", ex);
			return new ReadResult<T>(default, true, ErrorMessage);
		}
	}

	public async Task<WriteResult> SetValueAsync<T>(string address, T value, bool immediate = false, CancellationToken cts = default)
	{
		try
		{
			_cache[address] = value;

			if (immediate)
			{
				await _plcService.WriteVariableAsync(address, value, cts);
			}
			else
			{
				_writeQueue.Enqueue((new WriteRequest<T>(address, value), cts));
			}

			return new WriteResult(true);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			SetError($"Set failed for {address}", ex);
			return new WriteResult(false, ErrorMessage);
		}
	}

	private void SetError(string message, Exception ex)
	{
		lock (_errorLock)
		{
			_error = true;
			_lastErrorTime = DateTime.UtcNow;
			Volatile.Write(ref _errorMessage, $"{message}: {ex.Message}");
		}
		_logger.LogError(ex, message);
	}

	private void ClearError()
	{
		lock (_errorLock)
		{
			_error = false;
			Volatile.Write(ref _errorMessage, string.Empty);
		}
		_logger.LogInformation("PLC communication recovered");
	}

	public void Dispose()
	{
		_pollingTimer?.Dispose();
		GC.SuppressFinalize(this);
	}
}