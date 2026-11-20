using Microsoft.Maui.Networking;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Lets a test choose whether the device appears to be online.
/// </summary>
public class FakeConnectivity(NetworkAccess networkAccess = NetworkAccess.Internet) : IConnectivity
{
	public NetworkAccess NetworkAccess { get; set; } = networkAccess;

	public IEnumerable<ConnectionProfile> ConnectionProfiles => [];

	public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged
	{
		add { }
		remove { }
	}
}
