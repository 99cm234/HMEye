using HMEye.TwincatServices;

namespace HMEye.Services
{
	// To provide concurrent access, for client pages, to PLC variables.
	// Uses volatile types when variable / property primitive type.
	// More complex types should use Record type or other thread-safe collections.
	// Uses method to write variable to the PLC. Two different ways to write variables are shown:
	//		SetTemperaureSetpoint() for when variable updates at a high frequency.
	//		SetHumiditySetpointAsync() for when variable updates at a low frequency.
	// The slower method is far simpler and for usual PLC HMI use cases is adequate.
	public class PlcDataService : IHostedService, IDisposable
	{
		private readonly PlcService _plc;
		private readonly ILogger<PlcDataService> _logger;
		private readonly CancellationTokenSource _cts = new();
		private readonly Lock _lock = new();
		private Timer _timer = null!;
		private volatile bool _error;
		private volatile string _errorMessage = "";

		// Internal variables. Should be volatile. 
		private volatile short _temperature;
		private volatile short _pressure;
		private volatile short _temperatureSetpoint;
		private volatile short _temperatureSetpointOld;
		private volatile short _humiditySetpoint;

		// Public properties for client page access.
		public short Temperature => _temperature;
		public short Pressure => _pressure;
		public bool Error => _error;
		public string ErrorMessage => _errorMessage;
		public short TemperatureSetpoint => _temperatureSetpoint;
		public short HumiditySetpoint => _humiditySetpoint;

		public PlcDataService(ILogger<PlcDataService> logger, PlcService twincatAdsService)
		{
			_plc = twincatAdsService ?? throw new ArgumentNullException(nameof(twincatAdsService));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			cancellationToken.Register(() => _cts.Cancel());
			// prolly should put update timespan in config. But for now, nah.
			_timer = new Timer(async _ => await UpdateValues(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
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

		private async Task UpdateValues()
		{
			if (_cts.Token.IsCancellationRequested || _plc.IsConnected is false) return;
			try
			{
				var temp = await _plc.ReadVariableAsync<short>("MAIN.Temperature");
				var pressure = await _plc.ReadVariableAsync<short>("MAIN.Pressure");
				var temperatureSetpoint = await _plc.ReadVariableAsync<short>("MAIN.TemperatureSetpoint");
				var humiditySetpoint = await _plc.ReadVariableAsync<short>("MAIN.HumiditySetpoint");

				await UpdateTemperatureSetpointAsync(temperatureSetpoint);

				_temperature = temp;
				_pressure = pressure;
				_humiditySetpoint = humiditySetpoint;

				ClearError();
			}
			catch (Exception ex)
			{
				SetError($"Error updating PLC variables: {ex.Message}");
			}
		}

		private async Task UpdateTemperatureSetpointAsync(short plcTemperatureSetpoint)
		{
			// Always update _temperatureSetpoint to ensure sync with PLC
			Interlocked.Exchange(ref _temperatureSetpoint, plcTemperatureSetpoint);

			// if client updated setpoint, write that change to PLC
			// _temperatureSetpoint value changing while writing to PLC would be a mess, so a local variable is used to hold it.
			short currentSetpoint = _temperatureSetpoint;
			if (currentSetpoint != _temperatureSetpointOld)
			{
				await _plc.WriteVariableAsync<short>("MAIN.TemperatureSetpoint", currentSetpoint);
				Interlocked.Exchange(ref _temperatureSetpointOld, currentSetpoint);
			}
		}

		public void SetTemperatureSetpoint(short setpoint)
		{
			Interlocked.Exchange(ref _temperatureSetpoint, setpoint);
		}

		// variables with low frequency writes can be updated in a much simpler fashion:
		public async Task<bool> SetHumiditySetpointAsync(short setpoint)
		{
			try
			{
				await _plc.WriteVariableAsync("MAIN.HumiditySetpoint", setpoint);
				Interlocked.Exchange(ref _humiditySetpoint, setpoint);
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
}