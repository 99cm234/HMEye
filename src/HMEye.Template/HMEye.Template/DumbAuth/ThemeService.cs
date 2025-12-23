using HMEye.DumbAuth.Models;
using HMEye.DumbAuth.Themes;
using Microsoft.AspNetCore.Identity;
using MudBlazor;

namespace HMEye.DumbAuth;

public class ThemeService
{
	private readonly Dictionary<string, MudTheme> _themes = new()
	{
		{ "Default", DefaultTheme.Theme },
		{ "Black", BlackTheme.Theme },
		{ "Blue", BlueTheme.Theme },
		{ "M&M", MMTheme.Theme },
		{ "Skittles", SkittlesTheme.Theme },
	};

	public IReadOnlyDictionary<string, MudTheme> Themes => _themes;

	public MudTheme GetTheme(CustomUser? user)
	{
		if (user == null)
			return _themes["Default"];

		var themeName = string.IsNullOrEmpty(user.Theme) ? "Default" : user.Theme;
		if (!_themes.ContainsKey(themeName))
			themeName = "Default";

		return _themes[themeName];
	}

	public MudTheme GetTheme(string themeName)
	{
		if (string.IsNullOrEmpty(themeName) || !_themes.TryGetValue(themeName, out MudTheme? value))
			return _themes["Default"];
		return value;
	}

	public async Task UpdateUserThemeAsync(
		UserManager<CustomUser> userManager,
		CustomUser user,
		string themeName,
		bool isDarkMode
	)
	{
		user.Theme = _themes.ContainsKey(themeName) ? themeName : "Default";
		user.DarkMode = isDarkMode;
		await userManager.UpdateAsync(user);
	}
}
