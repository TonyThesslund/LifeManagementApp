using System.Globalization;
using LifeManagementApp.Converters;
using Microsoft.Maui.Graphics;

namespace LifeManagementApp.Tests;

public class PriorityToColorConverterTests
{
	private readonly PriorityToColorConverter _converter = new();

	private object? Convert(object? value) =>
		_converter.Convert(value, typeof(Color), null, CultureInfo.InvariantCulture);

	public static TheoryData<int, Color> PriorityColors => new()
	{
		{ 5, PriorityToColorConverter.Highest },
		{ 4, PriorityToColorConverter.High },
		{ 3, PriorityToColorConverter.Medium },
		{ 2, PriorityToColorConverter.Low },
		{ 1, PriorityToColorConverter.Lowest }
	};

	[Theory]
	[MemberData(nameof(PriorityColors))]
	public void Convert_GivesColorForEachPriority(int priority, Color expected)
	{
		Assert.Equal(expected, Convert(priority));
	}

	[Fact]
	public void Convert_EachPriority_HasDifferentColor()
	{
		var colors = Enumerable.Range(1, 5).Select(p => Convert(p)).Distinct().ToList();

		Assert.Equal(5, colors.Count);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(6)]
	[InlineData(-1)]
	[InlineData(null)]
	[InlineData("5")]
	public void Convert_UnexpectedValue_FallsBackToGray(object? value)
	{
		Assert.Equal(PriorityToColorConverter.Lowest, Convert(value));
	}

	[Fact]
	public void ConvertBack_IsNotSupported()
	{
		Assert.Throws<NotSupportedException>(() =>
			_converter.ConvertBack(PriorityToColorConverter.Highest, typeof(int), null, CultureInfo.InvariantCulture));
	}
}
