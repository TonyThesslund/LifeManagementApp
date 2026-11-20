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
	/// so the native window content is told the theme as well. The minimize, maximize and close
	/// buttons are drawn by Windows, so they get their colors here too.
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

		if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window { AppWindow.TitleBar: { } titleBar })
		{
			// System follows Windows, so the actual theme is asked from MAUI
			var dark = theme == ThemeSetting.Dark
				|| (theme == ThemeSetting.System && Application.Current?.PlatformAppTheme == AppTheme.Dark);

			var foreground = dark ? Windows.UI.Color.FromArgb(255, 0xE4, 0xEE, 0xF7) : Windows.UI.Color.FromArgb(255, 0x0F, 0x22, 0x33);
			var inactive = dark ? Windows.UI.Color.FromArgb(255, 0x93, 0xA9, 0xBE) : Windows.UI.Color.FromArgb(255, 0x51, 0x67, 0x7C);
			var hover = dark ? Windows.UI.Color.FromArgb(40, 255, 255, 255) : Windows.UI.Color.FromArgb(24, 0, 0, 0);
			var transparent = Windows.UI.Color.FromArgb(0, 0, 0, 0);

			titleBar.ButtonForegroundColor = foreground;
			titleBar.ButtonHoverForegroundColor = foreground;
			titleBar.ButtonPressedForegroundColor = foreground;
			titleBar.ButtonInactiveForegroundColor = inactive;
			titleBar.ButtonBackgroundColor = transparent;
			titleBar.ButtonInactiveBackgroundColor = transparent;
			titleBar.ButtonHoverBackgroundColor = hover;
			titleBar.ButtonPressedBackgroundColor = hover;
		}
#endif
	}
}
