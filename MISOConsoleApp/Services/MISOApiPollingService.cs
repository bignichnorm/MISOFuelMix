namespace MISOQueryingApp.Services;

public sealed class MISOApiPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MISOApiPollingService> _logger;
    private readonly TimeSpan _interval;

    public MISOApiPollingService(IServiceScopeFactory scopeFactory, ILogger<MISOApiPollingService> logger, TimeSpan? interval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_interval);

        await RunIngestAsync(cancellationToken);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await RunIngestAsync(cancellationToken);
        }
    }

    private async Task RunIngestAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IMISOFuelMixIngestionService>();
            await ingestionService.IngestAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fuel mix ingestion failed.");
        }
    }
}