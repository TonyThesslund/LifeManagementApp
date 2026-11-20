using System.Net;
using LifeManagementApp.Services;
using LifeManagementApp.Tests.Fakes;
using Microsoft.Maui.Networking;

namespace LifeManagementApp.Tests;

public class WeatherErrorHandlingTests
{
	private static Task<WeatherUnavailableException> GetErrorAsync(HttpMessageHandler handler, IConnectivity? connectivity = null)
	{
		var service = new OpenMeteoWeatherService(new HttpClient(handler), connectivity ?? new FakeConnectivity());
		return Assert.ThrowsAsync<WeatherUnavailableException>(() => service.GetCurrentWeatherAsync(63.10, 21.62));
	}

	[Theory]
	[InlineData(NetworkAccess.None)]
	[InlineData(NetworkAccess.Local)]
	[InlineData(NetworkAccess.ConstrainedInternet)]
	public async Task NoInternet_ThrowsNoInternet_WithoutCallingApi(NetworkAccess access)
	{
		var handler = new ThrowingHttpMessageHandler(new InvalidOperationException("Should not be called"));

		var error = await GetErrorAsync(handler, new FakeConnectivity(access));

		Assert.Equal(WeatherUnavailableException.NoInternetMessage, error.Message);
		Assert.False(handler.WasCalled);
	}

	[Fact]
	public async Task NetworkError_ThrowsUnreachable()
	{
		var error = await GetErrorAsync(new ThrowingHttpMessageHandler(new HttpRequestException("No such host")));

		Assert.Equal(WeatherUnavailableException.UnreachableMessage, error.Message);
		Assert.IsType<HttpRequestException>(error.InnerException);
	}

	[Fact]
	public async Task Timeout_ThrowsUnreachable()
	{
		var error = await GetErrorAsync(new ThrowingHttpMessageHandler(new TaskCanceledException("Timed out")));

		Assert.Equal(WeatherUnavailableException.UnreachableMessage, error.Message);
	}

	[Theory]
	[InlineData(HttpStatusCode.BadRequest)]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	public async Task ErrorStatusCode_ThrowsBadResponse(HttpStatusCode statusCode)
	{
		var error = await GetErrorAsync(new FakeHttpMessageHandler(statusCode, """{"error":true,"reason":"Bad"}"""));

		Assert.Equal(WeatherUnavailableException.BadResponseMessage, error.Message);
	}

	[Fact]
	public async Task InvalidJson_ThrowsBadResponse()
	{
		var error = await GetErrorAsync(new FakeHttpMessageHandler(HttpStatusCode.OK, "<html>Not JSON</html>"));

		Assert.Equal(WeatherUnavailableException.BadResponseMessage, error.Message);
	}

	[Fact]
	public async Task MissingCurrentWeather_ThrowsBadResponse()
	{
		var error = await GetErrorAsync(new FakeHttpMessageHandler(HttpStatusCode.OK, """{"latitude":63.1}"""));

		Assert.Equal(WeatherUnavailableException.BadResponseMessage, error.Message);
	}

	[Fact]
	public void Timeout_IsTenSeconds()
	{
		Assert.Equal(TimeSpan.FromSeconds(10), OpenMeteoWeatherService.Timeout);
	}
}
