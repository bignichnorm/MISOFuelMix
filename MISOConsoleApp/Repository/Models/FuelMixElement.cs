namespace MISOQueryingApp.Repository.Models;

public class FuelMixElement
{
    public long Id { get; set; }

    public long SnapshotId { get; set; }

    public string? Category { get; set; }

    public int MegaWatts { get; set; }
}
