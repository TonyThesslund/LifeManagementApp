namespace LifeManagementApp.Services;

/// <summary>
/// Thrown when the weather can't be fetched. The message is meant to be shown to the user.
/// </summary>
public class WeatherUnavailableException(string message, Exception? innerException = null)
	: Exception(message, innerException)
{
	public const string NoInternetMessage = "No internet connection.";
	public const string UnreachableMessage = "Could not reach the weather service.";
	public const string BadResponseMessage = "Weather data is not available right now.";
}
