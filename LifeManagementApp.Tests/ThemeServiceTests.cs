using LifeManagementApp.Models;
using LifeManagementApp.Services;
using Microsoft.Maui.ApplicationModel;

namespace LifeManagementApp.Tests;

public class ThemeServiceTests
{
	[Theory]
	[InlineData(ThemeSetting.System, AppTheme.Unspecified)]
	[InlineData(ThemeSetting.Light, AppTheme.Light)]
	[InlineData(ThemeSetting.Dark, AppTheme.Dark)]
	public void ToAppTheme_MapsEachSetting(ThemeSetting setting, AppTheme expected)
	{
		Assert.Equal(expected, ThemeService.ToAppTheme(setting));
	}
}
