using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MISOQueryingApp.DTOs;

namespace MISOQueryingApp.Tests;

public sealed class MISOFuelMixControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public MISOFuelMixControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFuelMix_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/fuel-mix");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFuelMix_ShouldReturnSnapshots()
    {
        var response = await _client.GetAsync("/api/fuel-mix");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MISOFuelMixResponse>();

        result.Should().NotBeNull();
        result.Fuel.Should().NotBeNull();
        result.Fuel.Type.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFuelMix_ShouldFilterByCategory()
    {
        var response = await _client.GetAsync("/api/fuel-mix?category=Wind");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MISOFuelMixResponse>();

        result.Should().NotBeNull();
        result.Fuel.Should().NotBeNull();
        result.Fuel.Type.Should().OnlyContain(x => x.Category == "Wind");
    }

    [Fact]
    public async Task GetFuelMix_ShouldFilterByDateRange()
    {
        var from = "2026-10-03T00:00:00Z";
        var to = "2026-10-04T00:00:00Z";
        var response = await _client.GetAsync($"/api/fuel-mix?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =  await response.Content.ReadFromJsonAsync<List<MISOFuelMixResponse>>();

        result.Should().NotBeNull();
        result.Should().AllSatisfy(x => x.ReferenceId.Should().NotBeNullOrEmpty());
        var referenceDates = result.Select(x => DateTimeOffset.Parse(x.ReferenceId)).ToList();
        referenceDates.Should().AllSatisfy(date => date.Should().BeOnOrAfter(DateTimeOffset.Parse(from)));
        referenceDates.Should().AllSatisfy(date => date.Should().BeBefore(DateTimeOffset.Parse(to)));
    }

    [Fact]
    public async Task GetFuelMix_ShouldRejectInvalidDateRange()
    {
        var response = await _client.GetAsync("/api/fuel-mix?from=2026-10-04T00:00:00Z&to=2026-10-03T00:00:00Z");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCategories_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/fuel-mix/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
