using System.Globalization;
using LifeManagementApp.Converters;

namespace LifeManagementApp.Tests;

public class BoolToTextConverterTests
{
	private readonly BoolToTextConverter _converter = new();

	private object? Convert(object? value, object? parameter) =>
		_converter.Convert(value, typeof(string), parameter, CultureInfo.InvariantCulture);

	[Fact]
	public void Convert_True_GivesFirstText()
	{
		Assert.Equal("Undo", Convert(true, "Undo|Mark done"));
	}

	[Fact]
	public void Convert_False_GivesSecondText()
	{
		Assert.Equal("Mark done", Convert(false, "Undo|Mark done"));
	}

	[Fact]
	public void Convert_NonBoolean_GivesSecondText()
	{
		Assert.Equal("Mark done", Convert(null, "Undo|Mark done"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("OnlyOne")]
	[InlineData("a|b|c")]
	public void Convert_InvalidParameter_FallsBackToValueText(string? parameter)
	{
		Assert.Equal("True", Convert(true, parameter));
	}

	[Fact]
	public void ConvertBack_IsNotSupported()
	{
		Assert.Throws<NotSupportedException>(() =>
			_converter.ConvertBack("Undo", typeof(bool), "Undo|Mark done", CultureInfo.InvariantCulture));
	}
}
