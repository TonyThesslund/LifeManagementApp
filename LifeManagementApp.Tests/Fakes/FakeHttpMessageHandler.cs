using System.Net;
using System.Text;

namespace LifeManagementApp.Tests.Fakes;

/// <summary>
/// Returns a fixed response instead of calling the network, and remembers the request it got.
/// </summary>
public class FakeHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
{
	public HttpRequestMessage? LastRequest { get; private set; }

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		LastRequest = request;

		return Task.FromResult(new HttpResponseMessage(statusCode)
		{
			Content = new StringContent(content, Encoding.UTF8, "application/json")
		});
	}
}
