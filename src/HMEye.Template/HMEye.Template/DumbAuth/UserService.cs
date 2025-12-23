using HMEye.DumbAuth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HMEye.DumbAuth;

public class UserService
{
	private readonly UserManager<CustomUser> _userManager;
	private readonly RoleManager<IdentityRole> _roleManager;

	public UserService(UserManager<CustomUser> userManager, RoleManager<IdentityRole> roleManager)
	{
		_userManager = userManager;
		_roleManager = roleManager;
	}

	public async Task<List<CustomUser>> GetUsersAsync()
	{
		return await _userManager.Users.ToListAsync();
	}

	public async Task<List<string>> GetRolesAsync()
	{
		return await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
	}

	public async Task<Dictionary<string, IList<string>>> GetUserRolesAsync(List<CustomUser> users)
	{
		var userRoles = new Dictionary<string, IList<string>>();
		foreach (var user in users)
		{
			userRoles[user.Id] = await _userManager.GetRolesAsync(user);
		}
		return userRoles;
	}
	public async Task<IList<string>> GetRolesForUserAsync(CustomUser user)
	{
		return await _userManager.GetRolesAsync(user);
	}

	public async Task<CustomUser?> GetUserByNameAsync(string? userName)
	{
		if (string.IsNullOrEmpty(userName))
			return null;
		return await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == userName);
	}

	public async Task<(bool Succeeded, string[] Errors)> AddUserAsync(
		string username,
		string email,
		string phoneNumber,
		string password,
		List<string> roles,
		bool darkMode,
		string theme,
		int expireTimeSpanMinutes
	)
	{
		var user = new CustomUser
		{
			UserName = username,
			Email = email,
			PhoneNumber = phoneNumber,
			DarkMode = darkMode,
			Theme = theme,
			ExpireTimeSpanMinutes = expireTimeSpanMinutes,
			TwoFactorEnabled = false,
		};

		var createResult = await _userManager.CreateAsync(user, password);
		if (!createResult.Succeeded)
			return (false, createResult.Errors.Select(e => e.Description).ToArray());

		foreach (var role in roles)
		{
			var roleResult = await _userManager.AddToRoleAsync(user, role);
			if (!roleResult.Succeeded)
				return (false, roleResult.Errors.Select(e => e.Description).ToArray());
		}
		return (true, Array.Empty<string>());
	}

	public async Task<(bool Succeeded, string[] Errors)> UpdateUserAsync(
		string userId,
		string email,
		string phoneNumber,
		List<string> roles,
		bool darkMode,
		string theme,
		int expireTimeSpanMinutes
	)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null)
			return (false, new[] { "User not found" });

		user.Email = email;
		user.PhoneNumber = phoneNumber;
		user.DarkMode = darkMode;
		user.Theme = theme;
		user.ExpireTimeSpanMinutes = expireTimeSpanMinutes;

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
			return (false, result.Errors.Select(e => e.Description).ToArray());

		var currentRoles = await _userManager.GetRolesAsync(user);
		var rolesToRemove = currentRoles.Except(roles).ToList();
		var rolesToAdd = roles.Except(currentRoles).ToList();

		var errors = new List<string>();

		if (rolesToRemove.Any())
		{
			var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
			if (!removeResult.Succeeded)
				errors.AddRange(removeResult.Errors.Select(e => e.Description).ToArray());
		}

		if (rolesToAdd.Any())
		{
			var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
			if (!addResult.Succeeded)
				errors.AddRange(addResult.Errors.Select(e => e.Description).ToArray());
		}

		return errors.Count == 0 ? (true, Array.Empty<string>()) : (false, errors.ToArray());
	}

	public async Task<(bool Succeeded, string[] Errors)> ChangeUserPasswordAsync(
		string userName,
		string currentPassword,
		string newPassword
	)
	{
		var user = await _userManager.FindByNameAsync(userName);
		if (user is null)
			return (false, new[] { "User not found" });
		var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
		if (result.Succeeded)
			return (true, Array.Empty<string>());
		return (false, result.Errors.Select(e => e.Description).ToArray());
	}

	public async Task<(bool Succeeded, string[] Errors)> ResetUserPasswordAsync(string userName, string newPassword)
	{
		var user = await _userManager.FindByNameAsync(userName);
		if (user is null)
			return (false, new[] { "User not found" });
		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
		if (result.Succeeded)
			return (true, Array.Empty<string>());
		return (false, result.Errors.Select(e => e.Description).ToArray());
	}

	public async Task<bool> DeleteUserAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user is null)
			return false;

		var result = await _userManager.DeleteAsync(user);
		return result.Succeeded;
	}
}
