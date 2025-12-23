using HMEye.DumbAuth.ConnectionTracking;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HMEye.DumbAuth.RateLimiting;
using HMEye.DumbAuth.Models;

namespace HMEye.DumbAuth;

public static class DumbAuthExtensions
{
	public static IServiceCollection AddDumbAuth(this IServiceCollection services, IConfiguration configuration, string appDataDir)
	{
		// Ensure the directory exists (optional, if not guaranteed elsewhere)
		Directory.CreateDirectory(appDataDir);
		string dbPath = Path.Combine(appDataDir, "DumbAuth.db");

		// Register DbContext
		services.AddDbContext<DumbAuthDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

		// Configure Identity services
		services
			.AddIdentity<CustomUser, IdentityRole>(options =>
			{
				options.Password.RequireDigit = false;
				options.Password.RequiredLength = 6;
				options.Password.RequireLowercase = false;
				options.Password.RequireUppercase = false;
				options.Password.RequireNonAlphanumeric = false;
				options.User.RequireUniqueEmail = false;
				options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
				options.Lockout.MaxFailedAccessAttempts = 5;
			})
			.AddRoles<IdentityRole>()
			.AddEntityFrameworkStores<DumbAuthDbContext>()
			.AddDefaultTokenProviders();

		// Configure cookie settings
		services.ConfigureApplicationCookie(options =>
		{
			options.LoginPath = "/account/login";
			options.LogoutPath = "/account/logout";
			options.AccessDeniedPath = "/account/access-denied";
			options.ExpireTimeSpan = TimeSpan.FromDays(7);
			options.SlidingExpiration = true;
		});

		// Register additional services
		services.AddScoped<UserService>();
		services.AddScoped<ThemeService>();
		services.AddScoped<IdentitySeederService>();
		services.AddSingleton<ConnectionTracker>();
		services.AddSingleton<UserSessionTracker>();
		services.AddScoped<CircuitHandler, ConnectionLimitHandler>();
		services.AddCustomRateLimiting(configuration);

		// Authorization and cascading authentication
		services.AddAuthorization();
		services.AddCascadingAuthenticationState();
		services.AddHostedService<DumbAuthInitializerService>();

		return services;
	}
}
