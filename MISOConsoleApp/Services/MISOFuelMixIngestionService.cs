using MISOQueryingApp.Client;
using MISOQueryingApp.Repository;
using MISOQueryingApp.Repository.Models;
using System.Globalization;

namespace MISOQueryingApp.Services;

public class MISOFuelMixIngestionService : IMISOFuelMixIngestionService
{
    private readonly IMISOFuelMixClient _client;
    private readonly AppDbContext _db;
    private readonly ILogger<MISOFuelMixIngestionService> _logger;

    public MISOFuelMixIngestionService(IMISOFuelMixClient client, AppDbContext db, ILogger<MISOFuelMixIngestionService> logger)
    {
        _client = client;
        _db = db;
        _logger = logger;
    }

    public async Task IngestAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MISO fuel mix ingestion.");
        var response = await _client.GetFuelMixSnapshotAsync(cancellationToken);

        if (!int.TryParse(response.TotalMegaWatts, out var totalMegaWatts))
        {
            throw new InvalidOperationException($"Invalid total mega watts: {response.TotalMegaWatts}");
        }

        var referenceIdFormat = "dd-MMM-yyyy - 'Interval' HH:mm 'EST'";
        if (!DateTime.TryParseExact(response.ReferenceId, referenceIdFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var estDateTime))
        {
            throw new InvalidOperationException($"Invalid MISO reference id: {response.ReferenceId}");
        }

        if (response.Fuel?.Type is null || !response.Fuel.Type.Any())
        {
            throw new InvalidOperationException("No fuel data available for ingestion.");
        }

        var intervalEst = new DateTimeOffset(estDateTime, TimeSpan.FromHours(-1));
        var snapshot = new FuelMixSnapshot
        {
            IntervalEst = intervalEst,
            TotalMegaWatts = totalMegaWatts,
            FuelMixElements = []
        };

        foreach (var item in response.Fuel.Type)
        {
            if (string.IsNullOrWhiteSpace(item.Category))
            {
                _logger.LogError("Missing fuel category value. Skipping item.");
                continue;
            }

            if (!int.TryParse(item.MegaWatts, out var megaWatts))
            {
                _logger.LogError("Invalid or missing fuel mega watts value: {}. Skipping item.", item.MegaWatts);
                continue;
            }

            snapshot.FuelMixElements.Add(
                new FuelMixElement
                {
                    Category = item.Category,
                    MegaWatts = megaWatts
                }
            );
        }

        _db.FuelMixSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("MISO fuel mix ingestion succeeded for {SnapshotIntervalEst}", snapshot.IntervalEst);
    }
}
