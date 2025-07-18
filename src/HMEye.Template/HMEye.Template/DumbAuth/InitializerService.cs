namespace HMEye.DumbAuth;

public class DumbAuthInitializerService : IHostedService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<DumbAuthInitializerService> _logger;

	public DumbAuthInitializerService(IServiceProvider serviceProvider, ILogger<DumbAuthInitializerService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		using var scope = _serviceProvider.CreateScope();
		var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeederService>();
		try
		{
			await seeder.SeedAsync();
			_logger.LogInformation("DumbAuth identities seeded successfully.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to seed DumbAuth identities.");
			throw; // Re-throw to halt startup if seeding is critical, or handle gracefully based on requirements
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
