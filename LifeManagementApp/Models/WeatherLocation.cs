namespace LifeManagementApp.Models;

/// <param name="Details">Region and country, e.g. "Ostrobothnia, Finland". Tells apart places with the same name.</param>
public record WeatherLocation(string Name, double Latitude, double Longitude, string? Details = null)
{
	public static WeatherLocation Vaasa { get; } = new("Vaasa", 63.10, 21.62, "Ostrobothnia, Finland");
}
