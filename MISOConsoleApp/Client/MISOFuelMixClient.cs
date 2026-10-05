using MISOQueryingApp.DTOs;

namespace MISOQueryingApp.Client;

public class MISOFuelMixClient : IMISOFuelMixClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MISOFuelMixClient> _logger;

    public MISOFuelMixClient(HttpClient httpClient, ILogger<MISOFuelMixClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MISOFuelMixResponse> GetFuelMixSnapshotAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/api/FuelMix", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MISOFuelMixResponse>(cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException("MISO returned an empty response.");
        }

        _logger.LogInformation("MISO fuel mix snapshot fetch succeeded.");

        return result;
    }
}
