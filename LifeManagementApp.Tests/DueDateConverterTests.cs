using System.Globalization;
using LifeManagementApp.Converters;
using LifeManagementApp.Models;
using LifeManagementApp.Tests.Fakes;

namespace LifeManagementApp.Tests;

public class DueDateConverterTests
{
	// Friday 20 November 2026, 14:30
	private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 11, 20, 14, 30, 0, TimeSpan.Zero));

	private readonly DueDateTextConverter _text = new() { Time = Time };
	private readonly DueStatusConverter _status = new() { Time = Time };

	private static TodoItem Due(int year, int month, int day, bool done = false) =>
		new() { Title = "Test", DueDate = new DateTime(year, month, day), IsDone = done };

	private object? Text(object? value) => _text.Convert(value, typeof(string), null, CultureInfo.InvariantCulture);

	private object? Status(object? value) => _status.Convert(value, typeof(DueStatus), null, CultureInfo.InvariantCulture);

	[Fact]
	public void DueYesterday_IsOverdue()
	{
		var item = Due(2026, 11, 19);

		Assert.Equal("Overdue", Text(item));
		Assert.Equal(DueStatus.Overdue, Status(item));
	}

	[Fact]
	public void DueLastYear_IsOverdue()
	{
		Assert.Equal("Overdue", Text(Due(2025, 12, 31)));
	}

	[Fact]
	public void DueToday_IsToday()
	{
		var item = Due(2026, 11, 20);

		Assert.Equal("Today", Text(item));
		Assert.Equal(DueStatus.Today, Status(item));
	}

	[Fact]
	public void DueToday_WithTimeOfDay_IsStillToday()
	{
		var item = new TodoItem { DueDate = new DateTime(2026, 11, 20, 9, 0, 0) };

		Assert.Equal("Today", Text(item));
	}

	[Fact]
	public void DueTomorrow_IsTomorrow()
	{
		var item = Due(2026, 11, 21);

		Assert.Equal("Tomorrow", Text(item));
		Assert.Equal(DueStatus.Tomorrow, Status(item));
	}

	[Fact]
	public void DueLaterThisYear_ShowsDayAndDate()
	{
		var item = Due(2026, 11, 26);

		Assert.Equal("Thu 26 Nov", Text(item));
		Assert.Equal(DueStatus.Later, Status(item));
	}

	[Fact]
	public void DueNextYear_ShowsYear()
	{
		Assert.Equal("5 Jan 2027", Text(Due(2027, 1, 5)));
	}

	[Fact]
	public void DateIsInEnglish_WhateverTheComputersLanguage()
	{
		var original = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = new CultureInfo("fi-FI");
		try
		{
			Assert.Equal("Thu 26 Nov", _text.Convert(Due(2026, 11, 26), typeof(string), null, CultureInfo.CurrentCulture));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[Fact]
	public void DoneTodo_PastDueDate_ShowsDateInsteadOfOverdue()
	{
		var item = Due(2026, 11, 19, done: true);

		Assert.Equal("Thu 19 Nov", Text(item));
		Assert.Equal(DueStatus.Done, Status(item));
	}

	[Fact]
	public void DoneTodo_DueToday_ShowsDate()
	{
		Assert.Equal("Fri 20 Nov", Text(Due(2026, 11, 20, done: true)));
	}

	[Fact]
	public void NewDay_ChangesLabel()
	{
		var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 20, 23, 59, 0, TimeSpan.Zero));
		var converter = new DueDateTextConverter { Time = time };
		var item = Due(2026, 11, 21);
		Assert.Equal("Tomorrow", converter.Convert(item, typeof(string), null, CultureInfo.InvariantCulture));

		time.Now = time.Now.AddMinutes(2);

		Assert.Equal("Today", converter.Convert(item, typeof(string), null, CultureInfo.InvariantCulture));
	}

	[Fact]
	public void NotATodo_GivesEmptyTextAndLater()
	{
		Assert.Equal(string.Empty, Text(null));
		Assert.Equal(DueStatus.Later, Status("not a to-do"));
	}

	[Fact]
	public void ConvertBack_IsNotSupported()
	{
		Assert.Throws<NotSupportedException>(() =>
			_text.ConvertBack("Today", typeof(TodoItem), null, CultureInfo.InvariantCulture));
		Assert.Throws<NotSupportedException>(() =>
			_status.ConvertBack(DueStatus.Today, typeof(TodoItem), null, CultureInfo.InvariantCulture));
	}
}
