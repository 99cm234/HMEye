using Microsoft.EntityFrameworkCore;

namespace HMEye.DumbTs;

public class DatabaseInitializerService : IHostedService
{
	private readonly IDbContextFactory<DumbTsDbContext> _dbContextFactory;
	private readonly ILogger<DatabaseInitializerService> _logger;

	public DatabaseInitializerService(
		IDbContextFactory<DumbTsDbContext> dbContextFactory,
		ILogger<DatabaseInitializerService> logger
	)
	{
		_dbContextFactory = dbContextFactory;
		_logger = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
		await context.Database.MigrateAsync(cancellationToken);
		_logger.LogInformation("DumbTs database tables verified/created");
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}