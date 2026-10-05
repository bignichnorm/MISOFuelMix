using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MISOQueryingApp.Client;
using Moq;

namespace MISOQueryingApp.Tests;

public class MISOFuelMixClientTests
{
    [Fact]
    public async Task GetFuelMixSnapshotAsync_ShouldDeserializeValidMISOResponse()
    {
        var json = """
        {
          "RefId": "03-Oct-2026 - Interval 12:00 EST",
          "TotalMW": "74523",
          "Fuel": {
            "Type": [
              {
                "INTERVALEST": "2026-10-03 12:00:00 PM",
                "CATEGORY": "Coal",
                "ACT": "16674",
                "FUEL_CATEGORY": "Coal  (16,674 MW)"
              },
              {
                "INTERVALEST": "2026-10-03 12:00:00 PM",
                "CATEGORY": "Wind",
                "ACT": "13789",
                "FUEL_CATEGORY": "Wind  (13,789 MW)"
              }
            ]
          }
        }
        """;

        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            }
        );

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test")
        };

        var client = new MISOFuelMixClient(httpClient, Mock.Of<ILogger<MISOFuelMixClient>>());

        var result = await client.GetFuelMixSnapshotAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.ReferenceId.Should().Be("03-Oct-2026 - Interval 12:00 EST");

        result.TotalMegaWatts.Should().Be("74523");

        result.Fuel.Should().NotBeNull();
        result.Fuel.Type.Should().HaveCount(2);

        result.Fuel.Type[0].Category.Should().Be("Coal");
        result.Fuel.Type[0].MegaWatts.Should().Be("16674");

        result.Fuel.Type[1].Category.Should().Be("Wind");
        result.Fuel.Type[1].MegaWatts.Should().Be("13789");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetFuelMixSnapshotAsync_ShouldThrowForNonRetryableStatus(
        HttpStatusCode statusCode)
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(statusCode));

        var client = CreateClient(handler);

        var act = () => client.GetFuelMixSnapshotAsync(CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetFuelMixSnapshotAsync_ShouldThrowForMalformedJson()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid json")
            }
        );

        var client = CreateClient(handler);

        var act = () => client.GetFuelMixSnapshotAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetFuelMixSnapshotAsync_ShouldRejectMissingFuelData()
    {
        var handler = new FakeHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "RefId": "test",
                      "TotalMW": "100",
                      "Fuel": {
                        "Type": []
                      }
                    }
                    """)
            });

        var client = CreateClient(handler);

        var act = () => client.GetFuelMixSnapshotAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static MISOFuelMixClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test")
        };

        return new MISOFuelMixClient(httpClient, Mock.Of<ILogger<MISOFuelMixClient>>());
    }
}
