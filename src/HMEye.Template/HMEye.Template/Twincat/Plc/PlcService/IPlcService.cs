using TwinCAT.Ads;

namespace HMEye.Twincat.Plc.PlcService
{
	/// <summary>
	/// Provides thread-safe access to TwinCAT PLC using TwinCAT ADS.
	/// Handles communication with the PLC, including reading and writing variables and arrays.
	/// </summary>
	/// <remarks>
	/// Service should be registered in Program.cs as a singleton and hosted service using the ITwincatPlcService interface:
	/// <code>
	/// builder.Services.AddSingleton<IPlcService, PlcService>();
	/// builder.Services.AddHostedService<IPlcService>(sp => sp.GetRequiredService<IPlcService>());
	/// </code>
	/// </remarks>
	public interface IPlcService : IDisposable, IHostedService
	{
		/// <summary>
		/// Event raised when the PLC connection is successfully established.
		/// </summary>
		event EventHandler? ConnectionSuccess;

		/// <summary>
		/// Event raised when the PLC connection is lost.
		/// </summary>
		event EventHandler? ConnectionLost;

		/// <summary>
		/// Indicates whether the PLC connection is currently active.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Reads the device state from the PLC.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task that resolves to the <see cref="ResultReadDeviceState"/> containing the device state.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task<ResultReadDeviceState> ReadDeviceStateAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Reads the device information from the PLC.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task that resolves to the <see cref="ResultDeviceInfo"/> containing the device information.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task<ResultDeviceInfo> ReadDeviceInfoAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Sends a command to the PLC, such as Start, Stop, or Reset.
		/// </summary>
		/// <param name="adsState">The command to send to the PLC as an <see cref="AdsState"/> enum value.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous command operation.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task PlcCommandAsync(AdsState adsState, CancellationToken cancellationToken = default);

		/// <summary>
		/// Reads a variable from the PLC.
		/// </summary>
		/// <typeparam name="T">The type of the variable to read. Ensure that the PLC and .NET data types are compatible.</typeparam>
		/// <param name="variableName">The name of the PLC variable to read, e.g., "MAIN.myInt".</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task that resolves to the value of the PLC variable as type <typeparamref name="T"/>.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the variable is not found in the PLC symbols.</exception>
		/// <exception cref="InvalidOperationException">Thrown when reading the variable fails.</exception>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task<T> ReadAsync<T>(string variableName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Writes a variable to the PLC.
		/// </summary>
		/// <typeparam name="T">The type of the variable to write. Ensure that the PLC and .NET data types are compatible.</typeparam>
		/// <param name="variableName">The name of the PLC variable to write, e.g., "MAIN.myInt".</param>
		/// <param name="value">The value to write to the PLC variable.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous write operation.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the variable is not found in the PLC symbols.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when a string value exceeds the maximum allowed length.</exception>
		/// <exception cref="InvalidOperationException">Thrown when writing the variable fails or data type is invalid.</exception>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task WriteAsync<T>(string variableName, T value, CancellationToken cancellationToken = default) where T : notnull;

		/// <summary>
		/// Reads a variable from the PLC using SymbolLoaderFactorty in Dynamic mode.
		/// </summary>
		/// <typeparam name="T">The type of the variable to read. Ensure that the PLC and .NET data types are compatible.</typeparam>
		/// <param name="variableName">The name of the PLC variable to read, e.g., "MAIN.myInt".</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task that resolves to the value of the PLC variable as type <typeparamref name="T"/>.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the variable is not found in the PLC symbols.</exception>
		/// <exception cref="InvalidOperationException">Thrown when reading the variable fails.</exception>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task<T> ReadDynamicAsync<T>(string variableName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Writes a variable to the PLC using SymbolLoaderFactorty in Dynamic mode.
		/// </summary>
		/// <typeparam name="T">The type of the variable to write. Ensure that the PLC and .NET data types are compatible.</typeparam>
		/// <param name="variableName">The name of the PLC variable to write, e.g., "MAIN.myInt".</param>
		/// <param name="value">The value to write to the PLC variable.</param>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task representing the asynchronous write operation.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the variable is not found in the PLC symbols.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when a string value exceeds the maximum allowed length.</exception>
		/// <exception cref="InvalidOperationException">Thrown when writing the variable fails or data type is invalid.</exception>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task WriteDynamicAsync<T>(string variableName, T value, CancellationToken cancellationToken = default) where T : notnull;
	}
}