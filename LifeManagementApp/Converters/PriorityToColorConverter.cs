using System.Globalization;

namespace LifeManagementApp.Converters;

/// <summary>
/// Gives each priority (1–5) a badge color. The colors are mid-tones that work with dark text
/// in both light and dark mode.
/// </summary>
public class PriorityToColorConverter : IValueConverter
{
	public static readonly Color Highest = Color.FromArgb("#EF5350");  // 5: red
	public static readonly Color High = Color.FromArgb("#FFA726");     // 4: orange
	public static readonly Color Medium = Color.FromArgb("#FFEE58");   // 3: yellow
	public static readonly Color Low = Color.FromArgb("#66BB6A");      // 2: green
	public static readonly Color Lowest = Color.FromArgb("#BDBDBD");   // 1: gray

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value switch
		{
			5 => Highest,
			4 => High,
			3 => Medium,
			2 => Low,
			_ => Lowest
		};
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException("Priority color is only for display.");
	}
}
