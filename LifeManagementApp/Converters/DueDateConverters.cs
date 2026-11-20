using System.Globalization;
using LifeManagementApp.Models;

namespace LifeManagementApp.Converters;

/// <summary>How a to-do's due date relates to today.</summary>
public enum DueStatus
{
	/// <summary>Due after tomorrow.</summary>
	Later,
	Tomorrow,
	Today,
	/// <summary>Due before today and not done yet.</summary>
	Overdue,
	/// <summary>Already done, so the due date no longer matters.</summary>
	Done
}

/// <summary>
/// Works out the due date label of a to-do: "Overdue", "Today", "Tomorrow", or otherwise the date,
/// e.g. "Thu 26 Nov" (with the year if it's not this year).
/// </summary>
public static class DueDateLabel
{
	public static DueStatus GetStatus(TodoItem item, DateTime today)
	{
		if (item.IsDone)
		{
			return DueStatus.Done;
		}

		var days = (item.DueDate.Date - today.Date).Days;
		return days switch
		{
			< 0 => DueStatus.Overdue,
			0 => DueStatus.Today,
			1 => DueStatus.Tomorrow,
			_ => DueStatus.Later
		};
	}

	public static string GetText(TodoItem item, DateTime today)
	{
		return GetStatus(item, today) switch
		{
			DueStatus.Overdue => "Overdue",
			DueStatus.Today => "Today",
			DueStatus.Tomorrow => "Tomorrow",
			_ => FormatDate(item.DueDate, today)
		};
	}

	// Invariant culture: the app's text is English, so day and month names are too
	private static string FormatDate(DateTime date, DateTime today)
	{
		var format = date.Year == today.Year ? "ddd d MMM" : "d MMM yyyy";
		return date.ToString(format, CultureInfo.InvariantCulture);
	}
}

/// <summary>Shows a to-do's due date as "Overdue", "Today", "Tomorrow" or the date. Binds to the whole <see cref="TodoItem"/>.</summary>
public class DueDateTextConverter : IValueConverter
{
	/// <summary>Where "today" comes from. Tests set a fixed time.</summary>
	public TimeProvider Time { get; set; } = TimeProvider.System;

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is TodoItem item
			? DueDateLabel.GetText(item, Time.GetLocalNow().Date)
			: string.Empty;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException("The due date label is only for display.");
	}
}

/// <summary>
/// Gives a to-do's <see cref="DueStatus"/>, so the page can color the label with triggers
/// (red when overdue). Binds to the whole <see cref="TodoItem"/>.
/// </summary>
public class DueStatusConverter : IValueConverter
{
	/// <summary>Where "today" comes from. Tests set a fixed time.</summary>
	public TimeProvider Time { get; set; } = TimeProvider.System;

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return value is TodoItem item
			? DueDateLabel.GetStatus(item, Time.GetLocalNow().Date)
			: DueStatus.Later;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotSupportedException("The due status is only for display.");
	}
}
