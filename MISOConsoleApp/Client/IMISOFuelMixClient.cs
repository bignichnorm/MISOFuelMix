using MISOQueryingApp.DTOs;

namespace MISOQueryingApp.Client;

public interface IMISOFuelMixClient
{
    public Task<MISOFuelMixResponse> GetFuelMixSnapshotAsync(CancellationToken cancellationToken);
}
