using Microsoft.EntityFrameworkCore;

namespace HMEye.DumbTs;

/// <summary>
/// Adds DumbTs logging services to the specified <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>Configures necessary services for DumbTs logging: database context factories,
/// hosted services for database initialization and maintenance, and the <see cref="DumbTsLogger"/>
/// singleton. Uses SQLite database.</remarks>
public static class DumbTsExtensions
{
	public static IServiceCollection AddDumbTsLogging(
		this IServiceCollection services,
		TimeSpan writeInterval,
		string? databaseDirectory = null,
		int commandTimeoutSeconds = 30
	)
	{
		var databasePath = Path.Combine(databaseDirectory ?? AppContext.BaseDirectory, "DumbTsData.db");

		services.AddDbContextFactory<DumbTsDbContext>(options =>
		{
			var connectionString = $"Data Source={databasePath}";
			options.UseSqlite(
				connectionString,
				sqliteOptions =>
				{
					sqliteOptions.CommandTimeout(commandTimeoutSeconds);
				}
			);
		});

		services.AddHostedService<DatabaseInitializerService>();

		services.AddSingleton<DumbTsLogger>(provider => new DumbTsLogger(
			provider.GetRequiredService<IDbContextFactory<DumbTsDbContext>>(),
			provider.GetRequiredService<ILogger<DumbTsLogger>>(),
			writeInterval
		));

		services.AddHostedService<DatabaseMaintenanceService>();

		return services;
	}
}