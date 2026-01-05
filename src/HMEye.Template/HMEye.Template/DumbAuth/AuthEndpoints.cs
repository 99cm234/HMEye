using System.ComponentModel.DataAnnotations;
using HMEye.DumbAuth.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace HMEye.DumbAuth;

public static class AuthEndpoints
{
	public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/auth");

		group
			.MapPost(
				"/login",
				[EnableRateLimiting("LoginPolicy")]
				async (
					HttpContext context,
					SignInManager<CustomUser> signInManager,
					UserManager<CustomUser> userManager
				) =>
				{
					var form = await context.Request.ReadFormAsync();
					var model = new LoginModel
					{
						Username = form["Username"].ToString(),
						Password = form["Password"].ToString(),
					};

					var validationResults = new List<ValidationResult>();
					var validationContext = new ValidationContext(model);
					bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

					if (!isValid)
					{
						var errors = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
						return Results.Redirect(
							$"/account/login?error={Uri.EscapeDataString("Invalid input: " + errors)}"
						);
					}

					var user = await userManager.FindByNameAsync(model.Username.Trim());
					if (user is null)
						return Results.Redirect("/account/login?error=user-not-found");

					if (await userManager.IsLockedOutAsync(user))
						return Results.Redirect("/account/login?error=account-locked");

					var passwordValid = await userManager.CheckPasswordAsync(user, model.Password);
					if (!passwordValid)
					{
						await userManager.AccessFailedAsync(user);
						return Results.Redirect("/account/login?error=invalid-password");
					}

					await userManager.ResetAccessFailedCountAsync(user);

					var expireTimeSpanMinutes = user.ExpireTimeSpanMinutes;
					var authProperties = new AuthenticationProperties
					{
						IsPersistent = model.RememberMe && expireTimeSpanMinutes > 0,
						ExpiresUtc =
							expireTimeSpanMinutes > 0 ? DateTimeOffset.UtcNow.AddMinutes(expireTimeSpanMinutes) : null,
					};
					await signInManager.SignInAsync(user, authProperties);
					return Results.Redirect("/");
				}
			)
			.AllowAnonymous();

		group
			.MapPost(
				"/logout",
				[EnableRateLimiting("LoginPolicy")]
				async (HttpContext context, SignInManager<CustomUser> signInManager) =>
				{
					await signInManager.SignOutAsync();
					return Results.Redirect("/account/login");
				}
			)
			.AllowAnonymous();
	}
}
