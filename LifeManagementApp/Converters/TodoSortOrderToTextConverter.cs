using System.Globalization;
using LifeManagementApp.Models;

namespace LifeManagementApp.Converters;

/// <summary>
/// Turns a <see cref="TodoSortOrder"/> into the text shown in the sort picker.
/// </summary>
public class TodoSortOrderToTextConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value switch
		{
			TodoSortOrder.Priority => "Priority",
			TodoSortOrder.DueDate => "Due date",
			_ => value?.ToString() ?? string.Empty
		};
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException("Sort order text is only for display.");
	}
}
