using System.Globalization;
using LifeManagementApp.Converters;
using LifeManagementApp.Models;

namespace LifeManagementApp.Tests;

public class TodoSortOrderToTextConverterTests
{
	private readonly TodoSortOrderToTextConverter _converter = new();

	private object? Convert(object? value) =>
		_converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

	[Theory]
	[InlineData(TodoSortOrder.Priority, "Priority")]
	[InlineData(TodoSortOrder.DueDate, "Due date")]
	public void Convert_GivesReadableText(TodoSortOrder sortOrder, string expected)
	{
		Assert.Equal(expected, Convert(sortOrder));
	}

	[Fact]
	public void Convert_EveryEnumValue_HasText()
	{
		foreach (var sortOrder in Enum.GetValues<TodoSortOrder>())
		{
			Assert.False(string.IsNullOrWhiteSpace(Convert(sortOrder) as string));
		}
	}

	[Fact]
	public void Convert_UnexpectedValue_DoesNotCrash()
	{
		Assert.Equal("something", Convert("something"));
		Assert.Equal(string.Empty, Convert(null));
	}

	[Fact]
	public void ConvertBack_IsNotSupported()
	{
		Assert.Throws<NotSupportedException>(() =>
			_converter.ConvertBack("Priority", typeof(TodoSortOrder), null, CultureInfo.InvariantCulture));
	}
}
