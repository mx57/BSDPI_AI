using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BSDPI.Core.Services.Warp;
using Xunit;

namespace BSDPI.Core.Tests;

public class WarpServiceTests
{
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }

    [Fact]
    public async Task RegisterAsync_ReturnsValidConfig()
    {
        var mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"config\": {\"peers\": [{\"public_key\": \"MOCK_PUBLIC_KEY\"}], \"interface\": {\"addresses\": {\"v4\": \"172.16.0.2/32\", \"v6\": \"fd01:5ca1::1/128\"}}}}",
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
            return Task.FromResult(response);
        });

        var httpClient = new HttpClient(mockHandler);
        var service = new WarpService(httpClient);
        var config = await service.RegisterAsync();

        Assert.NotNull(config);
        Assert.NotEmpty(config.PrivateKey);
        Assert.Equal("MOCK_PUBLIC_KEY", config.PublicKey);
        Assert.Equal("172.16.0.2/32", config.AddressV4);
        Assert.Equal("fd01:5ca1::1/128", config.AddressV6);
    }

    [Fact]
    public async Task RegisterAsync_WhenApiFails_ReturnsFallbackConfig()
    {
        var mockHandler = new MockHttpMessageHandler((req, ct) =>
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        });

        var httpClient = new HttpClient(mockHandler);
        var service = new WarpService(httpClient);
        var config = await service.RegisterAsync();

        Assert.NotNull(config);
        Assert.NotEmpty(config.PrivateKey);
        Assert.Equal("172.16.0.2/32", config.AddressV4);
        Assert.Equal("fd01:5ca1:ab1e:8273:c71:153e:d632:155e/128", config.AddressV6);
    }

    [Fact]
    public void GenerateWireGuardConfig_ReturnsValidString()
    {
        var service = new WarpService(new HttpClient());
        var config = new WarpConfig
        {
            PrivateKey = "PRIV",
            PublicKey = "PUB",
            AddressV4 = "1.2.3.4/32",
            AddressV6 = "::1/128",
            Endpoint = "end:123"
        };

        var result = service.GenerateWireGuardConfig(config);

        Assert.Contains("[Interface]", result);
        Assert.Contains("PrivateKey = PRIV", result);
        Assert.Contains("Address = 1.2.3.4/32, ::1/128", result);
        Assert.Contains("[Peer]", result);
        Assert.Contains("PublicKey = PUB", result);
        Assert.Contains("Endpoint = end:123", result);
    }
}
