using LifeManagementApp.Models;

namespace LifeManagementApp.Services;

public interface IGeocodingService
{
	/// <summary>Places whose name matches <paramref name="name"/>. Empty when nothing matches.</summary>
	/// <exception cref="LocationSearchException">The search could not be done.</exception>
	Task<IReadOnlyList<WeatherLocation>> SearchAsync(string name);
}
