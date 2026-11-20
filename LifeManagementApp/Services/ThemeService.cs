using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

/// <summary>
/// Applies the theme through MAUI's UserAppTheme, so pages using AppThemeBinding update immediately.
/// On Windows the native controls (text fields, pickers) are also switched, see <see cref="ApplyToWindows"/>.
/// </summary>
public class ThemeService : IThemeService
{
	public void Apply(ThemeSetting theme)
	{
		if (Application.Current is not { } app)
		{
			return;
		}

		app.UserAppTheme = ToAppTheme(theme);

		foreach (var window in app.Windows)
		{
			ApplyToWindows(window, theme);
		}
	}

	/// <summary>System becomes Unspecified, which makes MAUI follow the Windows setting.</summary>
	public static AppTheme ToAppTheme(ThemeSetting theme) => theme switch
	{
		ThemeSetting.Light => AppTheme.Light,
		ThemeSetting.Dark => AppTheme.Dark,
		_ => AppTheme.Unspecified
	};

	/// <summary>
	/// UserAppTheme changes MAUI's colors, but WinUI's own controls keep drawing in the Windows theme.
	/// In Light mode on a dark Windows, text fields then become invisible on the white page,
	/// so the native window content is told the theme as well.
	/// </summary>
	public static void ApplyToWindows(Window window, ThemeSetting theme)
	{
#if WINDOWS
		if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window { Content: Microsoft.UI.Xaml.FrameworkElement content })
		{
			content.RequestedTheme = theme switch
			{
				ThemeSetting.Light => Microsoft.UI.Xaml.ElementTheme.Light,
				ThemeSetting.Dark => Microsoft.UI.Xaml.ElementTheme.Dark,
				_ => Microsoft.UI.Xaml.ElementTheme.Default
			};
		}
#endif
	}
}
