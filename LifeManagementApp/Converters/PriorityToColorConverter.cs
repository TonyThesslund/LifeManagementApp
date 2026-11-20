using System.Globalization;

namespace LifeManagementApp.Converters;

/// <summary>
/// Gives each priority (1–5) a badge color. The colors are mid-tones that work with dark text
/// in both light and dark mode.
/// </summary>
public class PriorityToColorConverter : IValueConverter
{
	public static readonly Color Highest = Color.FromArgb("#E5484D");  // 5: red
	public static readonly Color High = Color.FromArgb("#F76B15");     // 4: orange
	public static readonly Color Medium = Color.FromArgb("#E2A400");   // 3: yellow
	public static readonly Color Low = Color.FromArgb("#30A46C");      // 2: green
	public static readonly Color Lowest = Color.FromArgb("#8B8D98");   // 1: gray

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
