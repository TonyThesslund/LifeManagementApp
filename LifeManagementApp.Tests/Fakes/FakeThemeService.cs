using LifeManagementApp.Models;
using LifeManagementApp.Services;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Records which themes were applied instead of changing the app.
/// </summary>
public class FakeThemeService : IThemeService
{
	public List<ThemeSetting> Applied { get; } = [];

	public void Apply(ThemeSetting theme) => Applied.Add(theme);
}
