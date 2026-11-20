namespace LifeManagementApp.Services;

/// <summary>
/// Thrown when a city search can't be done. The message is meant to be shown to the user.
/// </summary>
public class LocationSearchException(string message, Exception? innerException = null)
	: Exception(message, innerException)
{
	public const string NoInternetMessage = "No internet connection.";
	public const string UnreachableMessage = "Could not reach the location search.";
	public const string BadResponseMessage = "Location search is not available right now.";
}
