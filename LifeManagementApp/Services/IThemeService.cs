using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface IThemeService
{
	/// <summary>Switches the app to the given theme straight away.</summary>
	void Apply(ThemeSetting theme);
}
