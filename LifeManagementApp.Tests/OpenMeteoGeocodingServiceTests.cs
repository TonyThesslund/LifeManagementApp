using System.Net;
using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.Tests.Fakes;
using Microsoft.Maui.Networking;

namespace LifeManagementApp.Tests;

public class OpenMeteoGeocodingServiceTests
{
	// Shortened real response from https://geocoding-api.open-meteo.com/v1/search?name=Vaasa
	private const string VaasaJson = """
		{
		  "results": [
		    { "id": 632978, "name": "Vaasa", "latitude": 63.096, "longitude": 21.61577,
		      "country": "Finland", "admin1": "Ostrobothnia" },
		    { "id": 1, "name": "Vaasa", "latitude": 10.5, "longitude": 20.5,
		      "country": "Elsewhere" }
		  ],
		  "generationtime_ms": 0.5
		}
		""";

	private static OpenMeteoGeocodingService CreateService(HttpMessageHandler handler, IConnectivity? connectivity = null) =>
		new(new HttpClient(handler), connectivity ?? new FakeConnectivity());

	private static Task<LocationSearchException> GetErrorAsync(HttpMessageHandler handler, IConnectivity? connectivity = null) =>
		Assert.ThrowsAsync<LocationSearchException>(() => CreateService(handler, connectivity).SearchAsync("Vaasa"));

	[Fact]
	public async Task Search_MapsResults()
	{
		var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.OK, VaasaJson));

		var results = await service.SearchAsync("Vaasa");

		Assert.Equal(
			[
				new WeatherLocation("Vaasa", 63.096, 21.61577, "Ostrobothnia, Finland"),
				new WeatherLocation("Vaasa", 10.5, 20.5, "Elsewhere")
			],
			results);
	}

	[Fact]
	public async Task Search_NoMatches_ReturnsEmpty()
	{
		var service = CreateService(new FakeHttpMessageHandler(HttpStatusCode.OK, """{"generationtime_ms":0.3}"""));

		Assert.Empty(await service.SearchAsync("Xyzzyville"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task Search_EmptyText_ReturnsEmpty_WithoutCallingApi(string name)
	{
		var handler = new ThrowingHttpMessageHandler(new InvalidOperationException("Should not be called"));

		Assert.Empty(await CreateService(handler).SearchAsync(name));
		Assert.False(handler.WasCalled);
	}

	[Fact]
	public async Task Search_SendsEscapedTrimmedName()
	{
		var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, VaasaJson);

		await CreateService(handler).SearchAsync("  Mäntsälä town ");

		var url = handler.LastRequest!.RequestUri!.AbsoluteUri;
		Assert.StartsWith("https://geocoding-api.open-meteo.com/v1/search?", url);
		Assert.Contains("name=M%C3%A4nts%C3%A4l%C3%A4%20town&", url);
		Assert.Contains($"count={OpenMeteoGeocodingService.MaxResults}", url);
	}

	[Fact]
	public async Task NoInternet_ThrowsNoInternet_WithoutCallingApi()
	{
		var handler = new ThrowingHttpMessageHandler(new InvalidOperationException("Should not be called"));

		var error = await GetErrorAsync(handler, new FakeConnectivity(NetworkAccess.None));

		Assert.Equal(LocationSearchException.NoInternetMessage, error.Message);
		Assert.False(handler.WasCalled);
	}

	[Fact]
	public async Task NetworkError_ThrowsUnreachable()
	{
		var error = await GetErrorAsync(new ThrowingHttpMessageHandler(new HttpRequestException("No such host")));

		Assert.Equal(LocationSearchException.UnreachableMessage, error.Message);
	}

	[Fact]
	public async Task Timeout_ThrowsUnreachable()
	{
		var error = await GetErrorAsync(new ThrowingHttpMessageHandler(new TaskCanceledException("Timed out")));

		Assert.Equal(LocationSearchException.UnreachableMessage, error.Message);
	}

	[Theory]
	[InlineData(HttpStatusCode.BadRequest, """{"error":true}""")]
	[InlineData(HttpStatusCode.OK, "not json")]
	[InlineData(HttpStatusCode.OK, "null")]
	public async Task BadResponse_ThrowsBadResponse(HttpStatusCode statusCode, string content)
	{
		var error = await GetErrorAsync(new FakeHttpMessageHandler(statusCode, content));

		Assert.Equal(LocationSearchException.BadResponseMessage, error.Message);
	}

	[Fact]
	public void Timeout_IsTenSeconds()
	{
		Assert.Equal(TimeSpan.FromSeconds(10), OpenMeteoGeocodingService.Timeout);
	}
}
