using HMEye.DumbAuth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HMEye.DumbAuth;

public class IdentitySeederService
{
	private readonly IServiceProvider _services;
	private readonly IConfiguration _configuration;

	public IdentitySeederService(IServiceProvider services, IConfiguration configuration)
	{
		_services = services;
		_configuration = configuration;
	}

	public async Task SeedAsync()
	{
		using var scope = _services.CreateScope();
		var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CustomUser>>();
		var dbContext = scope.ServiceProvider.GetRequiredService<DumbAuthDbContext>();

		// Ensure database is created and migrated
		dbContext.Database.Migrate();

		// Seed roles
		string[] roles = ["Admin", "User"];
		foreach (var role in roles)
		{
			if (!await roleManager.RoleExistsAsync(role))
				await roleManager.CreateAsync(new IdentityRole(role));
		}

		// Seed users
		var adminUsername = _configuration["Admin:Username"] ?? "admin";
		var userUsername = _configuration["User:Username"] ?? "user";

		var admin = await userManager.FindByNameAsync(adminUsername);
		var user = await userManager.FindByNameAsync(userUsername);

		if (admin == null)
		{
			admin = new CustomUser
			{
				UserName = adminUsername,
				Email = _configuration["Admin:Email"] ?? null,
				PhoneNumber = _configuration["Admin:PhoneNumber"] ?? null,
				DarkMode = true,
				Theme = "Black",
			};

			var result = await userManager.CreateAsync(admin, _configuration["Admin:Password"] ?? "Eeyore");
			if (!result.Succeeded)
				throw new Exception(
					"Failed to create admin: " + string.Join(", ", result.Errors.Select(e => e.Description))
				);

			await userManager.AddToRoleAsync(admin, "Admin");
		}
		if (user == null)
		{
			user = new CustomUser
			{
				UserName = userUsername,
				Email = _configuration["User:Email"] ?? null,
				PhoneNumber = _configuration["User:PhoneNumber"] ?? null,
				DarkMode = false,
				Theme = "M&M",
			};

			var result = await userManager.CreateAsync(user, _configuration["User:Password"] ?? "Piglet");
			if (!result.Succeeded)
				throw new Exception(
					"Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description))
				);

			await userManager.AddToRoleAsync(user, "User");
		}
	}
}
