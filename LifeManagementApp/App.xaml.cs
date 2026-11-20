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

	// The page column is at most 808 wide (see PageColumn in Styles.xaml), plus some room on both sides.
	// On a screen that is too low for the height, the window is made to fit (see CenterOnScreen).
	private const double WindowWidth = 880;
	private const double WindowHeight = 1000;

	// Room left free above and below the window on a low screen, e.g. for the taskbar
	private const double ScreenMargin = 80;

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell())
		{
			Title = AppName,
			Width = WindowWidth,
			Height = WindowHeight
		};
		CenterOnScreen(window);

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

	/// <summary>Places the window in the middle of the main screen, made lower if the screen is too low for it.</summary>
	private static void CenterOnScreen(Window window)
	{
		var display = DeviceDisplay.Current.MainDisplayInfo;
		if (display.Density <= 0)
		{
			return;
		}

		// The screen size is in pixels, the window size in scaled units
		var screenWidth = display.Width / display.Density;
		var screenHeight = display.Height / display.Density;
		window.Height = Math.Min(window.Height, screenHeight - ScreenMargin);
		window.X = Math.Max(0, (screenWidth - window.Width) / 2);
		window.Y = Math.Max(0, (screenHeight - window.Height) / 2);
	}
}
