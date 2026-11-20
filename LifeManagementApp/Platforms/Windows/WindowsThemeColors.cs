using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WinColor = Windows.UI.Color;

namespace LifeManagementApp.Platforms.Windows;

/// <summary>
/// Windows draws some parts of the controls itself: the list that opens from a picker, the calendar,
/// the background of text fields and pickers, and the back button. Those take their colors from WinUI's
/// theme resources, which are replaced here with the "Kite" colors from Resources/Styles/Colors.xaml.
/// <para>
/// The colors are set in code for the current theme, because WinUI ignored Light/Dark theme dictionaries
/// in Platforms/Windows/App.xaml (MAUI's own resources win). Called before the window's theme changes,
/// so the controls pick up the new colors when they redraw for the new theme.
/// </para>
/// </summary>
public static class WindowsThemeColors
{
	// Kite colors: Surface, SurfaceAlt, a slightly darker SurfaceAlt for pressed, Border, a stronger border for hover
	private static readonly Palette Light = new(
		Surface: Hex(0xFBF8F2), SurfaceAlt: Hex(0xEFE8DB), Pressed: Hex(0xE6DCCB),
		Border: Hex(0xE0D6C4), BorderHover: Hex(0xD2C6B0), Accent: Hex(0x0369A1));

	private static readonly Palette Dark = new(
		Surface: Hex(0x0F1C2A), SurfaceAlt: Hex(0x142536), Pressed: Hex(0x1B3047),
		Border: Hex(0x20354A), BorderHover: Hex(0x2C4660), Accent: Hex(0x38BDF8));

	public static void Apply(bool dark)
	{
		var resources = Microsoft.UI.Xaml.Application.Current.Resources;
		var c = dark ? Dark : Light;
		var transparent = WinColor.FromArgb(0, 0, 0, 0);

		var brushes = new Dictionary<string, WinColor>
		{
			// Pickers, closed
			["ComboBoxBackground"] = c.Surface,
			["ComboBoxBackgroundPointerOver"] = c.SurfaceAlt,
			["ComboBoxBackgroundPressed"] = c.Pressed,
			["ComboBoxBackgroundFocused"] = c.Surface,
			["ComboBoxBorderBrush"] = c.Border,
			["ComboBoxBorderBrushPointerOver"] = c.BorderHover,

			// Picker lists
			["ComboBoxDropDownBackground"] = c.Surface,
			["ComboBoxDropDownBorderBrush"] = c.Border,
			["ComboBoxItemBackgroundPointerOver"] = c.SurfaceAlt,
			["ComboBoxItemBackgroundPressed"] = c.Pressed,
			["ComboBoxItemBackgroundSelected"] = c.SurfaceAlt,
			["ComboBoxItemBackgroundSelectedPointerOver"] = c.Pressed,
			["ComboBoxItemBackgroundSelectedPressed"] = c.Pressed,
			["ComboBoxItemPillFillBrush"] = c.Accent,

			// Text fields
			["TextControlBackground"] = c.Surface,
			["TextControlBackgroundPointerOver"] = c.SurfaceAlt,
			["TextControlBackgroundFocused"] = c.Surface,
			["TextControlBorderBrush"] = c.Border,
			["TextControlBorderBrushPointerOver"] = c.BorderHover,

			// Date picker and its calendar
			["CalendarDatePickerBackground"] = c.Surface,
			["CalendarDatePickerBackgroundPointerOver"] = c.SurfaceAlt,
			["CalendarDatePickerBackgroundPressed"] = c.Pressed,
			["CalendarDatePickerBorderBrush"] = c.Border,
			["CalendarDatePickerBorderBrushPointerOver"] = c.BorderHover,
			["CalendarViewBackground"] = c.Surface,
			["CalendarViewBorderBrush"] = c.Border,
			["CalendarViewCalendarItemBackground"] = c.Surface,
			["CalendarViewNavigationButtonBackground"] = transparent,

			// Back button in the title bar
			["NavigationViewBackButtonBackground"] = transparent,
			["NavigationViewButtonBackgroundPointerOver"] = c.SurfaceAlt,
			["NavigationViewButtonBackgroundPressed"] = c.Pressed,
		};

		foreach (var (key, color) in brushes)
		{
			resources[key] = new SolidColorBrush(color);
		}
	}

	private static WinColor Hex(int rgb) =>
		WinColor.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

	private sealed record Palette(
		WinColor Surface, WinColor SurfaceAlt, WinColor Pressed, WinColor Border, WinColor BorderHover, WinColor Accent);
}
