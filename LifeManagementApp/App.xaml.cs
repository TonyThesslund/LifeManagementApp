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

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());

		// The native window doesn't exist yet in the constructor, so its theme is set once it's created
		window.Created += (_, _) => ThemeService.ApplyToWindows(window, _settings.Theme);

		return window;
	}
}
