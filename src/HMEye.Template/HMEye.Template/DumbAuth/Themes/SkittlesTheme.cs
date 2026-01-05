using MudBlazor;

namespace HMEye.DumbAuth.Themes;

public class SkittlesTheme
{
	public static readonly MudTheme Theme = new()
	{
		PaletteDark = new PaletteDark
		{
			Primary = "#9B30FF", // Purple Skittle
			Secondary = "#e64808", // Orange Skittle
			Tertiary = "#048207", // Green Skittle
			Surface = "#362136", // Subdued purple
			Background = "#220022", // Almost black purple
			BackgroundGray = "#2A002A", // Purple Skittle
			AppbarText = "#FFFFFF", // White for readability
			AppbarBackground = "rgba(0,0,0,0.7)",
			DrawerBackground = "rgba(0,0,0,0.7)",
			ActionDefault = "#f1be02", // Yellow Skittle
			ActionDisabled = "#9999994D", // Semi-transparent gray
			ActionDisabledBackground = "#605F6D4D",
			TextPrimary = "#FFFFFF",
			TextSecondary = "#f1be02", // Yellow Skittle
			DrawerIcon = "#f1be02", // Yellow Skittle
			DrawerText = "#f1be02",
			TextDisabled = "#33FFFFFF",
			GrayLight = "#440044", // Purple Skittle
			GrayLighter = "#550D55", // Lighter purple
			Info = "#9B30FF",
			Success = "#3DCB6C",
			Warning = "#FFB545",
			Error = "#FF3F5F",
			LinesDefault = "#633368",
			TableLines = "#633368",
			Divider = "#292838",
			OverlayLight = "#1E1E2D80",
		},

		PaletteLight = new PaletteLight
		{
			Primary = "#9B30FF", // Purple Skittle
			Secondary = "#e64808", // Orange Skittle
			Tertiary = "#048207", // Green Skittle
			Surface = "#EEEEEE", // Light gray for surfaces
			Background = "#F7F7F7", // Very light gray background
			BackgroundGray = "#E0E0E0", // Light gray for subtle contrast
			AppbarText = "#222222", // Dark gray for readability
			AppbarBackground = "#DDDDDD", // Light gray
			DrawerBackground = "#F2F2F2", // Light gray drawer
			ActionDefault = "#f1be02", // Yellow Skittle
			ActionDisabled = "#99999966",
			ActionDisabledBackground = "#B0B0B066",
			TextPrimary = "#222222", // Dark text on light background
			TextSecondary = "#9B30FF", // Purple Skittle
			TextDisabled = "#99999966",
			DrawerIcon = "#222222",
			DrawerText = "#222222",
			GrayLight = "#CCCCCC", // Standard light gray
			GrayLighter = "#E6E6E6", // Even lighter gray
			Info = "#9B30FF",
			Success = "#3DCB6C",
			Warning = "#FFB545",
			Error = "#FF3F5F",
			LinesDefault = "#CCCCCC",
			TableLines = "#CCCCCC",
			Divider = "#BBBBBB",
			OverlayLight = "#00000033",
		},
	};
}
