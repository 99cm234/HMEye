using TwinCAT.Ads;
using TwinCAT.SystemService;

namespace HMEye.Twincat.Plc.SystemService
{
	/// <summary>
	/// Provides thread-safe access to the TwinCAT system service using TwinCAT ADS.
	/// This service handles operations related to the system service, such as reading the system state.
	/// </summary>
	/// <remarks>
	/// Service should be registered in Program.cs as a singleton and hosted service using the ISystemService interface:
	/// <code>
	/// builder.Services.AddSingleton<ISystemService, SystemService>();
	/// builder.Services.AddHostedService<ISystemService>(sp => sp.GetRequiredService<ISystemService>());
	/// </code>
	/// </remarks>
	public interface ISystemService : IDisposable, IHostedService
	{
		/// <summary>
		/// Event raised when the system service connection is successfully established.
		/// </summary>
		event EventHandler? ConnectionSuccess;

		/// <summary>
		/// Event raised when the system service connection is lost.
		/// </summary>
		event EventHandler? ConnectionLost;

		/// <summary>
		/// Indicates whether the system service connection is currently active.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// Reads the system state from the TwinCAT system service.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
		/// <returns>A task that resolves to the <see cref="AdsSysServState"/> containing the system state.</returns>
		/// <exception cref="AdsErrorException">Thrown when an ADS-specific error occurs.</exception>
		/// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
		/// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
		/// <exception cref="Exception">Thrown when an unexpected error occurs.</exception>
		Task<AdsSysServState> ReadSystemStateAsync(CancellationToken cancellationToken = default);
	}
}