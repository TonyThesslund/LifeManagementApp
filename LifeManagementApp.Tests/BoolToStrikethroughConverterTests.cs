using System.Globalization;
using LifeManagementApp.Converters;

namespace LifeManagementApp.Tests;

public class BoolToStrikethroughConverterTests
{
	private readonly BoolToStrikethroughConverter _converter = new();

	private object? Convert(object? value) =>
		_converter.Convert(value, typeof(TextDecorations), null, CultureInfo.InvariantCulture);

	[Fact]
	public void Convert_True_GivesStrikethrough()
	{
		Assert.Equal(TextDecorations.Strikethrough, Convert(true));
	}

	[Fact]
	public void Convert_False_GivesNone()
	{
		Assert.Equal(TextDecorations.None, Convert(false));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("true")]
	[InlineData(1)]
	public void Convert_NonBoolean_GivesNone(object? value)
	{
		Assert.Equal(TextDecorations.None, Convert(value));
	}

	[Theory]
	[InlineData(TextDecorations.Strikethrough, true)]
	[InlineData(TextDecorations.None, false)]
	[InlineData(TextDecorations.Underline, false)]
	public void ConvertBack_GivesWhetherStruckThrough(TextDecorations decorations, bool expected)
	{
		Assert.Equal(expected, _converter.ConvertBack(decorations, typeof(bool), null, CultureInfo.InvariantCulture));
	}
}
