using Microsoft.EntityFrameworkCore;

namespace HMEye.DumbTs;

/// <summary>
/// A background service that performs periodic maintenance tasks on the DumbTs database.
/// </summary>
/// <remarks>Cleanup operations performed at regular intervals specified in the app config.
/// <para>CleanupInterval defaults to 24 hours if not set.</para>
/// <para>CutoffDays defaults to 30 if not set.</para>
/// <code>
/// {
///		"DumbTs" {
///			"CleanupInterval": 24,
///			"CutoffDays": 30
///		}
///	}
/// </code>
/// </remarks>
public class DatabaseMaintenanceService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<DatabaseMaintenanceService> _logger;
	private readonly IConfiguration _configuration;

	public DatabaseMaintenanceService(
		IServiceProvider serviceProvider,
		ILogger<DatabaseMaintenanceService> logger,
		IConfiguration configuration
	)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
		_configuration = configuration;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<DumbTsDbContext>();

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				int cutoffDays = int.TryParse(_configuration["DumbTs:CutoffDays"], out var parsedDays)
					? parsedDays
					: 30;
				var cutoff = DateTime.UtcNow.AddDays(-cutoffDays);
				var oldNumericRecords = await dbContext
					.NumericDataPoints.Where(p => p.Timestamp < cutoff)
					.ExecuteDeleteAsync(stoppingToken);

				var oldBooleanRecords = await dbContext
					.BooleanDataPoints.Where(p => p.Timestamp < cutoff)
					.ExecuteDeleteAsync(stoppingToken);

				var oldTextRecords = await dbContext
					.TextDataPoints.Where(p => p.Timestamp < cutoff)
					.ExecuteDeleteAsync(stoppingToken);

				_logger.LogInformation(
					"Removed {NumericCount} old numeric, {BooleanCount} old boolean, and {TextCount} old text DumbTs records",
					oldNumericRecords,
					oldBooleanRecords,
					oldTextRecords
				);
			}
			catch (Exception ex) when (ex is not TaskCanceledException)
			{
				_logger.LogError(ex, "DumbTs database maintenance failed");
				// perform cleanup
			}
			int cleanupIntervalHours = int.TryParse(_configuration["DumbTs:CleanupInterval"], out var parsedHours)
				? parsedHours
				: 24;
			await Task.Delay(TimeSpan.FromHours(cleanupIntervalHours), stoppingToken);
		}
	}
}