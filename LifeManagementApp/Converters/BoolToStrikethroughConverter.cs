using System.Globalization;

namespace LifeManagementApp.Converters;

/// <summary>
/// Strikes text through when the bound value is true, e.g. the title of a finished to-do.
/// </summary>
public class BoolToStrikethroughConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is true ? TextDecorations.Strikethrough : TextDecorations.None;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is TextDecorations decorations && decorations.HasFlag(TextDecorations.Strikethrough);
	}
}
