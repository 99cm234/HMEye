using MudBlazor;

namespace HMEye.DumbAuth.Themes;

public class BlackTheme
{
	public static readonly MudTheme Theme = new()
	{
		PaletteLight = new PaletteLight
		{
			Black = "#110e2d",
			AppbarText = "#424242",
			AppbarBackground = "cccccc",
			Background = "#a7a7a7",
			Surface = "#cccccc",
			DrawerBackground = "cccccc",
			GrayLight = "#e8e8e8",
			GrayLighter = "#f9f9f9",
		},
		PaletteDark = new PaletteDark
		{
			Primary = "#7e6fff", // Default theme primary color
			Surface = "#1c1c1c", // Very dark gray
			Background = "#121212", // Near black
			BackgroundGray = "#1a1a1a", // Slightly lighter black
			AppbarText = "#ffffff", // White for readability
			AppbarBackground = "rgba(0,0,0,0.8)", // Black, slightly translucent
			DrawerBackground = "rgba(0,0,0,0.8)", // Black, slightly translucent
			ActionDefault = "#b0b0b0", // Light gray for actions
			ActionDisabled = "#9999994d", // Semi-transparent gray
			ActionDisabledBackground = "#605f6d4d", // Semi-transparent gray
			TextPrimary = "#ffffff", // White for readability
			TextSecondary = "#b0b0b0", // Light gray for secondary text
			TextDisabled = "#33ffffff", // Semi-transparent white
			DrawerIcon = "#b0b0b0", // Light gray
			DrawerText = "#b0b0b0", // Light gray
			GrayLight = "#2a2a2a", // Dark gray
			GrayLighter = "#333333", // Slightly lighter gray
			Info = "#4a86ff", // Blue from default theme
			Success = "#3dcb6c", // Green from default theme
			Warning = "#ffb545", // Yellow from default theme
			Error = "#ff3f5f", // Red from default theme
			LinesDefault = "#3c3c3c", // Dark gray
			TableLines = "#3c3c3c", // Dark gray
			Divider = "#2a2a2a", // Dark gray
			OverlayLight = "#1c1c1c80", // Semi-transparent dark
		},
	};
}
