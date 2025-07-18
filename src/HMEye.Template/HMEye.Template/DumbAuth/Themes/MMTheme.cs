using MudBlazor;

namespace HMEye.DumbAuth.Themes;

public class MMTheme
{
	public static readonly MudTheme Theme = new()
	{
		PaletteLight = new PaletteLight
		{
			Primary = "#0072C6", // Blue M&M (same as dark)
			Secondary = "#FF4500", // Orange M&M (same as dark)
			Tertiary = "#3DCB6C", // Green M&M (same as dark)
			Success = "#3DCB6C", // Green M&M (same as dark)
			Warning = "#FFB545", // Original warning color (same as dark)
			Error = "#FF3F5F", // Original error color (same as dark)
			Surface = "#E6D2B5", // Light tan brown for surfaces
			Background = "#F5E8D3", // Soft light brown background
			BackgroundGray = "#D9C2A6", // Slightly darker light brown for subtle contrast
			AppbarText = "#333333", // Dark gray for readable text
			AppbarBackground = "#E6D2B5", // Light tan brown, opaque
			DrawerBackground = "#F5E8D3", // Soft light brown for drawer
			ActionDefault = "#FF4500", // Orange M&M (same as dark)
			ActionDisabled = "#99999966", // Semi-transparent gray
			ActionDisabledBackground = "#B0B0B066", // Lighter semi-transparent gray
			TextPrimary = "#333333", // Dark gray for primary text
			TextSecondary = "#555555", // Medium gray for secondary text
			TextDisabled = "#99999966", // Semi-transparent gray for disabled text
			DrawerIcon = "#333333", // Dark gray for icons
			DrawerText = "#333333", // Dark gray for drawer text
			GrayLight = "#999999", // Gray
			GrayLighter = "#E8E8E8", // Very light gray
			Info = "#0072C6", // Blue M&M (same as dark)
			LinesDefault = "#999999", // Gray for lines
			TableLines = "#999999", // Gray for table lines
			Divider = "#B8A78A", // Medium brown for dividers
			OverlayLight = "#00000033", // Semi-transparent dark overlay
		},
		PaletteDark = new PaletteDark
		{
			Primary = "#0072C6", // Blue M&M
			Secondary = "#FF4500", // Orange M&M
			Tertiary = "#3DCB6C", // Green M&M
			Surface = "#3A2A2A", // Slightly lighter brown
			Background = "#2A1A1A", // Dark brown background
			BackgroundGray = "#3A2A2A", // Slightly lighter brown
			AppbarText = "#FFD700", // Yellow M&M
			AppbarBackground = "rgba(0,0,0,0.7)", // Black, translucent
			DrawerBackground = "rgba(0,0,0,0.7)", // Black, translucent
			ActionDefault = "#FF4500", // Orange M&M
			ActionDisabled = "#9999994D", // Semi-transparent gray
			ActionDisabledBackground = "#605F6D4D", // Semi-transparent gray
			TextPrimary = "#FFFFFF", // White for readability
			TextSecondary = "#FFD700", // Yellow M&M
			TextDisabled = "#33FFFFFF", // Semi-transparent white
			DrawerIcon = "#FFD700", // Yellow M&M
			DrawerText = "#FFD700", // Yellow M&M
			GrayLight = "#3A2A2A", // Slightly lighter brown
			GrayLighter = "#4A3A3A", // Even lighter brown
			Info = "#0072C6", // Blue M&M
			Success = "#3DCB6C", // Original success color
			Warning = "#FFB545", // Original warning color
			Error = "#FF3F5F", // Original error color
			LinesDefault = "#4A3A3A", // Slightly lighter brown
			TableLines = "#4A3A3A", // Slightly lighter brown
			Divider = "#292838", // Dark gray
			OverlayLight = "#1E1E2D80", // Semi-transparent dark
		},
	};
}
