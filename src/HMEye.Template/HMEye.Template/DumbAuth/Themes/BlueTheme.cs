using MudBlazor;

namespace HMEye.DumbAuth.Themes;

public class BlueTheme
{
	public static readonly MudTheme Theme = new()
	{
		PaletteLight = new PaletteLight
		{
			Primary = "#1565C0", // Deep blue for prominence
			Secondary = "#26A69A", // Teal for contrast
			Tertiary = "#42A5F5", // Light vibrant blue
			Success = "#2E7D32", // Green from default MudBlazor
			Warning = "#F57C00", // Orange from default MudBlazor
			Error = "#D32F2F", // Red from default MudBlazor
			Background = "#ECEFF1", // Light grey-blue background
			BackgroundGray = "#CFD8DC", // Slightly darker grey-blue
			Surface = "#ECEFF1", // Light grey-blue for surfaces
			AppbarBackground = "#CFD8DC", // Slightly darker grey-blue for appbar
			AppbarText = "#212121", // Very dark gray for high contrast
			DrawerBackground = "#ECEFF1", // Light grey-blue for drawer
			DrawerText = "#212121", // Very dark gray for readability
			DrawerIcon = "#212121", // Very dark gray for icons
			ActionDefault = "#1565C0", // Match Primary for consistency
			ActionDisabled = "#75757566", // Semi-transparent gray
			ActionDisabledBackground = "#B0B0B066", // Lighter semi-transparent gray
			TextPrimary = "#212121", // Very dark gray for high contrast
			TextSecondary = "#424242", // Medium gray for secondary text
			TextDisabled = "#75757566", // Semi-transparent gray
			GrayLight = "#B0BEC5", // Light blue-gray
			GrayLighter = "#CFD8DC", // Very light blue-gray
			Info = "#0288D1", // Bright blue for info
			LinesDefault = "#B0BEC5", // Light blue-gray for lines
			TableLines = "#B0BEC5", // Light blue-gray for table lines
			Divider = "#B0BEC5", // Light blue-gray for dividers
			OverlayLight = "#00000033", // Semi-transparent dark overlay
		},
		PaletteDark = new PaletteDark
		{
			Primary = "#1565C0", // Deep blue for prominence
			Secondary = "#26A69A", // Teal for contrast
			Tertiary = "#42A5F5", // Light vibrant blue
			Success = "#2E7D32", // Green from default MudBlazor
			Warning = "#F57C00", // Orange from default MudBlazor
			Error = "#D32F2F", // Red from default MudBlazor
			Background = "#263238", // Dark blue-gray background
			BackgroundGray = "#37474F", // Slightly lighter blue-gray
			Surface = "#37474F", // Darker blue-gray for surfaces
			AppbarBackground = "#0D47A1", // Deep blue, opaque
			AppbarText = "#F5F5F5", // Light gray for high contrast
			DrawerBackground = "#263238", // Dark blue-gray for drawer
			DrawerText = "#F5F5F5", // Light gray for readability
			DrawerIcon = "#F5F5F5", // Light gray for icons
			ActionDefault = "#1565C0", // Match Primary for consistency
			ActionDisabled = "#B0BEC566", // Semi-transparent blue-gray
			ActionDisabledBackground = "#78909C66", // Semi-transparent blue-gray
			TextPrimary = "#F5F5F5", // Light gray for high contrast
			TextSecondary = "#B0BEC5", // Medium blue-gray for secondary text
			TextDisabled = "#B0BEC566", // Semi-transparent blue-gray
			GrayLight = "#78909C", // Medium blue-gray
			GrayLighter = "#B0BEC5", // Light blue-gray
			Info = "#0288D1", // Bright blue for info
			LinesDefault = "#78909C", // Medium blue-gray for lines
			TableLines = "#78909C", // Medium blue-gray for table lines
			Divider = "#78909C", // Medium blue-gray for dividers
			OverlayLight = "#00000080", // Semi-transparent dark overlay
		},
	};
}
