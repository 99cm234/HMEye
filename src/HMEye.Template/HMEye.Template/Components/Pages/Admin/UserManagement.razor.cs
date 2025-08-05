using HMEye.DumbAuth;
using HMEye.DumbAuth.Models;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace HMEye.Components.Pages.Admin;

public partial class UserManagement
{
	private List<CustomUser>? users;
	private List<string> roles = new();
	private string? editingUserId;
	private EditUserDialog.EditUserModel editUserModel = new();
	private AddUserDialog.NewUserModel newUserModel = new();
	private ResetPasswordDialog.ResetPasswordModel resetUserModel = new();
	private EditContext editUserEditContext = null!;
	private EditContext newUserEditContext = null!;
	private EditContext resetPasswordEditContext = null!;
	private List<string> errors = new();
	private bool showResetPasswordDialog;
	private bool showEditUserDialog;
	private bool showAddUserDialog;
	private CustomUser? currentUser;
	private Dictionary<string, IList<string>> userRoles = new();
	private DialogOptions dialogOptions = new()
	{
		CloseOnEscapeKey = true,
		BackdropClick = true,
		CloseButton = true,
	};

	protected override async Task OnInitializedAsync()
	{
		newUserEditContext = new EditContext(newUserModel);
		resetPasswordEditContext = new EditContext(resetUserModel);
		editUserEditContext = new EditContext(editUserModel);
		users = await UserService.GetUsersAsync();
		roles = await UserService.GetRolesAsync();
		userRoles = await UserService.GetUserRolesAsync(users);
		var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
		var identityUser = authState.User;

		if (identityUser.Identity?.IsAuthenticated == true)
		{
			var userName = identityUser.Identity.Name;
			if (userName is not null)
			{
				currentUser = await UserService.GetUserByNameAsync(userName);
			}
		}
	}

	private void BeginAddUser()
	{
		CloseAllDialogs();
		showAddUserDialog = true;
	}

	private async Task AddUser()
	{
		var (succeeded, addErrors) = await UserService.AddUserAsync(
			newUserModel.Username,
			newUserModel.Email!,
			newUserModel.PhoneNumber!,
			newUserModel.Password,
			newUserModel.Roles.ToList(),
			newUserModel.DarkMode,
			newUserModel.Theme,
			newUserModel.ExpireTimeSpanMinutes
		);
		if (succeeded)
		{
			CloseAllDialogs();
			await RefreshUsersAsync();
			newUserModel = new();
			newUserEditContext = new EditContext(newUserModel);
		}
		else
		{
			errors.AddRange(addErrors);
		}
	}

	private void CancelAddUser()
	{
		CloseAllDialogs();
		newUserModel = new();
		newUserEditContext = new EditContext(newUserModel);
	}

	private async Task DeleteUser(string userId)
	{
		if (currentUser is not null && userId == currentUser.Id)
		{
			errors.Add("You cannot delete your own account.");
			return;
		}
		await UserService.DeleteUserAsync(userId);
		CloseAllDialogs();
		await RefreshUsersAsync();
	}

	private void BeginEdit(CustomUser user)
	{
		CloseAllDialogs();
		editingUserId = user.Id;
		editUserModel = new EditUserDialog.EditUserModel
		{
			Username = user.UserName ?? "",
			Email = user.Email,
			PhoneNumber = user.PhoneNumber,
			Roles = userRoles.TryGetValue(user.Id, out var roles) ? roles.ToList() : new List<string>(),
			DarkMode = user.DarkMode,
			Theme = user.Theme ?? "",
			ExpireTimeSpanMinutes = user.ExpireTimeSpanMinutes,
		};
		editUserEditContext = new EditContext(editUserModel);
		showEditUserDialog = true;
	}

	private void CancelEdit()
	{
		CloseAllDialogs();
		editingUserId = null;
		editUserModel = new();
		editUserEditContext = new EditContext(editUserModel);
	}

	private async Task SaveChanges(string userId)
	{
		var (succeeded, updateErrors) = await UserService.UpdateUserAsync(
			userId,
			editUserModel.Email!,
			editUserModel.PhoneNumber!,
			editUserModel.Roles.ToList(),
			editUserModel.DarkMode,
			editUserModel.Theme,
			editUserModel.ExpireTimeSpanMinutes
		);
		if (succeeded)
		{
			CloseAllDialogs();
			await RefreshUsersAsync();
			editingUserId = null;
		}
		else
		{
			errors.AddRange(updateErrors);
		}
	}

	private void BeginResetPassword(CustomUser user)
	{
		CloseAllDialogs();
		resetUserModel = new ResetPasswordDialog.ResetPasswordModel
		{
			Username = user.UserName ?? "",
			NewPassword = "",
			ConfirmPassword = "",
		};
		resetPasswordEditContext = new EditContext(resetUserModel);
		showResetPasswordDialog = true;
	}

	private async Task ResetPassword()
	{
		var (succeeded, resetErrorsResult) = await UserService.ResetUserPasswordAsync(
			resetUserModel.Username ?? "",
			resetUserModel.NewPassword
		);
		if (succeeded)
		{
			CloseAllDialogs();
			await RefreshUsersAsync();
			resetUserModel = new();
			resetPasswordEditContext = new EditContext(resetUserModel);
		}
		else
		{
			errors.AddRange(resetErrorsResult);
		}
	}

	private void CancelResetPassword()
	{
		CloseAllDialogs();
		resetUserModel = new();
		resetPasswordEditContext = new EditContext(resetUserModel);
	}

	private void CloseAllDialogs()
	{
		showAddUserDialog = false;
		showEditUserDialog = false;
		showResetPasswordDialog = false;
		errors.Clear();
	}

	private async Task RefreshUsersAsync()
	{
		users = await UserService.GetUsersAsync();
		userRoles = await UserService.GetUserRolesAsync(users);
		roles = await UserService.GetRolesAsync();
	}
}
