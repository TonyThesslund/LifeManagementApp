using System.Globalization;

namespace LifeManagementApp.Converters;

/// <summary>
/// Chooses between two texts given as the converter parameter, "text if true|text if false".
/// Example: ConverterParameter='Undo|Mark done'.
/// </summary>
public class BoolToTextConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var texts = (parameter as string)?.Split('|');
		if (texts is not { Length: 2 })
		{
			return value?.ToString() ?? string.Empty;
		}

		return value is true ? texts[0] : texts[1];
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException("Text chosen from a bool is only for display.");
	}
}
