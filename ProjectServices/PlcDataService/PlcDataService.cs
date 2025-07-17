using HMEye.TwincatServices;

namespace HMEye.Services;

// Provides thread-safe access to PLC variables for Blazor client pages.
// Designed as a template service, prioritizing simplicity for end-users while ensuring thread safety.
// Relies on PlcService for thread-safe PLC communication with built-in error handling and cancellation.
// Uses a timer to periodically read PLC data, reducing redundant client-driven PLC traffic.
// Public methods (SetTemperatureSetpoint, SetHumiditySetpointAsync) are kept simple for user customization.
// Assumes 64-bit system and .NET 9.0 or newer, where reads and writes of short, double, bool, and string references are atomic.
// Two different ways to write variables are shown:
//		SetTemperatureSetpoint() for when variable updates at a high frequency.
//		SetHumiditySetpointAsync() for when variable updates at a low frequency.
// The slower method is far simpler and for usual PLC HMI use cases is adequate.
//
// Use of complex types can be done using immutable record types, but this is not recommended for high-frequency updates.
public class PlcDataService : IHostedService, IDisposable
{
	private readonly PlcService _plc;
	private readonly ILogger<PlcDataService> _logger;
	private readonly CancellationTokenSource _cts = new();
	private readonly object _lock = new();
	private Timer _timer = null!;
	private bool _error;
	private string _errorMessage = "";

	// Internal variables
	// add new variables here as needed.
	private double _temperature;
	private double _pressure;
	private short _temperatureSetpoint;
	private short _temperatureSetpointOld;
	private short _humiditySetpoint;
	private bool _isRunning;

	// Public properties for error state. Do not modify; used for service status reporting.
	public bool Error => _error;
	public string ErrorMessage => _errorMessage;

	// Public properties for client page access to PLC data. Safe to extend or modify for additional PLC variables.
	// Reads are atomic for short, double, and bool types on 64-bit systems in .NET 9.0 or newer.
	// Non-atomic types can use Interlocked operations or immutable records.
	public double Temperature => _temperature;
	public double Pressure => _pressure;
	public short TemperatureSetpoint => _temperatureSetpoint;
	public short HumiditySetpoint => _humiditySetpoint;
	public bool IsRunning => _isRunning;

	public PlcDataService(ILogger<PlcDataService> logger, PlcService twincatAdsService)
	{
		_plc = twincatAdsService ?? throw new ArgumentNullException(nameof(twincatAdsService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		cancellationToken.Register(() => _cts.Cancel());
		// prolly should put update timespan in config. But for now, nah.
		_timer = new Timer(async _ => await UpdateValuesAsync(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
		_logger.LogInformation("PlcDataService initialized");
		_plc.ConnectionLost += OnPlcConnectionLost;
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_cts.Cancel();
		_timer?.Change(Timeout.Infinite, Timeout.Infinite);
		return Task.CompletedTask;
	}

	private void OnPlcConnectionLost(object? sender, EventArgs e)
	{
		SetError("PLC connection lost");
	}

	// Periodic update of local state with PLC data to minimize client-driven PLC traffic.
	private async Task UpdateValuesAsync()
	{
		if (_cts.Token.IsCancellationRequested || !_plc.IsConnected)
			return;

		try
		{
			// All PLC reads in one batch to reduce PLC load.
			var tempTask = _plc.ReadVariableAsync<double>("MAIN.Temperature", _cts.Token);
			var pressureTask = _plc.ReadVariableAsync<double>("MAIN.Pressure", _cts.Token);
			var tempSetpointTask = _plc.ReadVariableAsync<short>("MAIN.TemperatureSetpoint", _cts.Token);
			var humiditySetpointTask = _plc.ReadVariableAsync<short>("MAIN.HumiditySetpoint", _cts.Token);
			var isRunningTask = _plc.ReadVariableAsync<bool>("MAIN.IsRunning", _cts.Token);

			await Task.WhenAll(tempTask, pressureTask, tempSetpointTask, humiditySetpointTask, isRunningTask);

			// Update local state with direct writes.
			_temperature = await tempTask;
			_pressure = await pressureTask;
			_humiditySetpoint = await humiditySetpointTask;
			_isRunning = await isRunningTask;

			// Sync temperature setpoint with PLC
			await UpdateTemperatureSetpointAsync(await tempSetpointTask);

			ClearError();
		}
		catch (Exception ex)
		{
			SetError($"Error updating PLC variables: {ex.Message}");
		}
	}

	// UpdateTemperatureSetpointAsync and SetTemperatureSetpoint are suggested for high-frequency updates.
	// Ensures PLC variable is updated only as necessary and with a limited frequency.
	private async Task UpdateTemperatureSetpointAsync(short plcTemperatureSetpoint)
	{
		// Always update _temperatureSetpoint to ensure sync with PLC
		_temperatureSetpoint = plcTemperatureSetpoint;

		// if client updated setpoint, write that change to PLC
		// Local variables ensure a consistent snapshot for comparison.
		short currentSetpoint = _temperatureSetpoint;
		short oldSetpoint = _temperatureSetpointOld;
		if (currentSetpoint != oldSetpoint)
		{
			await _plc.WriteVariableAsync<short>("MAIN.TemperatureSetpoint", currentSetpoint, _cts.Token);
			_temperatureSetpointOld = currentSetpoint;
		}
	}

	public void SetTemperatureSetpoint(short setpoint)
	{
		_temperatureSetpoint = setpoint;
	}

	// variables with low frequency writes can be updated in a much simpler fashion:
	public async Task<bool> SetHumiditySetpointAsync(short setpoint)
	{
		try
		{
			await _plc.WriteVariableAsync<short>("MAIN.HumiditySetpoint", setpoint, _cts.Token);
			_humiditySetpoint = setpoint;
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError("Error writing humidity setpoint: {ex.Message}", ex.Message);
			return false;
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

	public void Dispose()
	{
		_plc.ConnectionLost -= OnPlcConnectionLost;
		_cts.Dispose();
		_timer?.Dispose();
	}
}