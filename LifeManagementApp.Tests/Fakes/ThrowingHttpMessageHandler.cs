namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Simulates a failed request by throwing the given exception, like a network error or timeout.
/// </summary>
public class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
{
	public bool WasCalled { get; private set; }

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		WasCalled = true;
		throw exception;
	}
}
