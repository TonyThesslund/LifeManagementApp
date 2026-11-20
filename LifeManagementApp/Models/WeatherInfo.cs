namespace LifeManagementApp.Models;

public class WeatherInfo
{
	/// <summary>Air temperature 2 m above ground, in °C.</summary>
	public double Temperature { get; set; }

	/// <summary>Wind speed 10 m above ground, in m/s.</summary>
	public double WindSpeed { get; set; }

	/// <summary>Time the measurement applies to.</summary>
	public DateTime Time { get; set; }
}
