using LifeManagementApp.Services;

namespace LifeManagementApp;

public partial class App : Application
{
	private readonly ISettingsService _settings;

	public App(ISettingsService settings, IThemeService themeService)
	{
		InitializeComponent();
		_settings = settings;

		// Start in the theme the user chose last time
		themeService.Apply(settings.Theme);
	}

	public const string AppName = "Life Management";

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell())
		{
			Title = AppName
		};

#if WINDOWS
		window.TitleBar = CreateTitleBar();
#endif

		// The native window doesn't exist yet in the constructor, so its theme is set once it's created
		window.Created += (_, _) => ThemeService.ApplyToWindows(window, _settings.Theme);

		// With the System theme, Windows can switch between light and dark while the app is open
		RequestedThemeChanged += (_, _) => ThemeService.ApplyToWindows(window, _settings.Theme);

		return window;
	}

#if WINDOWS
	/// <summary>
	/// The window's title bar on Windows, in the app's own colors. Without it Windows draws its default
	/// title bar in the Windows theme, so in Light mode on a dark Windows the title was white on light grey.
	/// The colors follow the app theme, so they change together with the rest of the app.
	/// </summary>
	private TitleBar CreateTitleBar()
	{
		var titleBar = new TitleBar
		{
			Title = AppName,
			Icon = "logo.png",
			HeightRequest = 36
		};
		titleBar.SetAppThemeColor(TitleBar.BackgroundColorProperty, GetColor("ShellLight"), GetColor("ShellDark"));
		titleBar.SetAppThemeColor(TitleBar.ForegroundColorProperty, GetColor("TextLight"), GetColor("TextDark"));
		return titleBar;
	}

	private Color GetColor(string key) => (Color)Resources[key];
#endif
}
