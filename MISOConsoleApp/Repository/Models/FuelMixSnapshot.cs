namespace MISOQueryingApp.Repository.Models;

public class FuelMixSnapshot
{
    public long Id { get; set; }

    public DateTimeOffset IntervalEst { get; set; }

    public int TotalMegaWatts { get; set; }

    public ICollection<FuelMixElement> FuelMixElements { get; set; } = [];
}
