using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Tests.Fakes;

namespace LifeManagementApp.Tests;

public class PreferencesSettingsServiceTests
{
	private readonly FakePreferences _preferences = new();
	private readonly PreferencesSettingsService _settings;

	public PreferencesSettingsServiceTests()
	{
		_settings = new PreferencesSettingsService(_preferences);
	}

	[Fact]
	public void Defaults_WhenNothingSaved()
	{
		Assert.Equal(WeatherLocation.Vaasa, _settings.Location);
		Assert.Equal(5.0, _settings.WindLimit);
		Assert.Equal(ThemeSetting.System, _settings.Theme);
	}

	[Fact]
	public void Location_IsSavedAndReadBack()
	{
		var helsinki = new WeatherLocation("Helsinki", 60.17, 24.94);

		_settings.Location = helsinki;

		Assert.Equal(helsinki, _settings.Location);
	}

	[Fact]
	public void Location_IsReadBack_ByANewInstance()
	{
		_settings.Location = new WeatherLocation("Helsinki", 60.17, 24.94);

		var reopened = new PreferencesSettingsService(_preferences);

		Assert.Equal(new WeatherLocation("Helsinki", 60.17, 24.94), reopened.Location);
	}

	[Fact]
	public void WindLimit_IsSavedAndReadBack()
	{
		_settings.WindLimit = 7.5;

		Assert.Equal(7.5, new PreferencesSettingsService(_preferences).WindLimit);
	}

	[Theory]
	[InlineData(ThemeSetting.Light)]
	[InlineData(ThemeSetting.Dark)]
	[InlineData(ThemeSetting.System)]
	public void Theme_IsSavedAndReadBack(ThemeSetting theme)
	{
		_settings.Theme = theme;

		Assert.Equal(theme, new PreferencesSettingsService(_preferences).Theme);
	}

	[Fact]
	public void Theme_IsSavedAsText()
	{
		_settings.Theme = ThemeSetting.Dark;

		Assert.Contains("Dark", _preferences.Values.Values);
	}

	[Fact]
	public void Theme_UnknownSavedValue_FallsBackToSystem()
	{
		_preferences.Values["theme"] = "Purple";

		Assert.Equal(ThemeSetting.System, _settings.Theme);
	}
}
