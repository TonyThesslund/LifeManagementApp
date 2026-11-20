namespace LifeManagementApp.Models;

public record WeatherLocation(string Name, double Latitude, double Longitude)
{
	public static WeatherLocation Vaasa { get; } = new("Vaasa", 63.10, 21.62);
}
