namespace MISOQueryingApp.Services;

public interface IMISOFuelMixIngestionService
{
    Task IngestAsync(CancellationToken cancellationToken);
}
