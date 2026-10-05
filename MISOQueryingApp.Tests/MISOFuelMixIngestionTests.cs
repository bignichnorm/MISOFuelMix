using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MISOQueryingApp.Client;
using MISOQueryingApp.DTOs;
using MISOQueryingApp.Repository;
using MISOQueryingApp.Services;
using Moq;

namespace MISOQueryingApp.Tests;

public class FuelMixIngestionServiceTests
{
    [Fact]
    public async Task IngestAsync_ShouldPersistSnapshotAndReadings()
    {
        var client = new Mock<IMISOFuelMixClient>();
        var dbContext = CreateDbContext();

        var snapshot = new MISOFuelMixResponse
        {
            ReferenceId = new DateTimeOffset(2026, 10, 3, 16, 0, 0, TimeSpan.Zero).ToString(),
            TotalMegaWatts = "1000",
            Fuel = new MISOFuelList {
                Type = new List<MISOFuelItem> {
                    new MISOFuelItem {
                        Category = "Coal",
                        MegaWatts = "500"
                    },
                    new MISOFuelItem {
                        Category = "Wind",
                        MegaWatts = "500"
                    }
                }
            }
        };

        client.Setup(x => x.GetFuelMixSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var service = new MISOFuelMixIngestionService(client.Object, dbContext, Mock.Of<ILogger<MISOFuelMixIngestionService>>());

        await service.IngestAsync(CancellationToken.None);

        var savedSnapshot = await dbContext.FuelMixSnapshots.Include(x => x.FuelMixElements).SingleAsync();

        savedSnapshot.TotalMegaWatts.Should().Be(1000);
        savedSnapshot.IntervalEst.Should().Be(new DateTimeOffset(2026, 10, 3, 16, 0, 0, TimeSpan.FromHours(-1)));
        savedSnapshot.FuelMixElements.Should().HaveCount(2);
        savedSnapshot.FuelMixElements.Should().ContainSingle(x => x.Category == "Coal" && x.MegaWatts == 500);
        savedSnapshot.FuelMixElements.Should().ContainSingle(x => x.Category == "Wind" && x.MegaWatts == 500);
    }

    [Fact]
    public async Task IngestAsync_WhenClientFails_ShouldThrow()
    {
        var client = new Mock<IMISOFuelMixClient>();
        var dbContext = CreateDbContext();

        client
            .Setup(x => x.GetFuelMixSnapshotAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MISO unavailable"));

        var service = new MISOFuelMixIngestionService(client.Object, dbContext, Mock.Of<ILogger<MISOFuelMixIngestionService>>());

        var act = () => service.IngestAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("MISO unavailable");

        dbContext.FuelMixSnapshots.Should().BeEmpty();
    }

    [Fact]
    public async Task IngestAsync_ShouldPreserveNegativeGenerationValues()
    {
        var client = new Mock<IMISOFuelMixClient>();
        var dbContext = CreateDbContext();

        var response = new MISOFuelMixResponse
        {
            ReferenceId = new DateTimeOffset(2026, 10, 3, 16, 0, 0, TimeSpan.Zero).ToString(),
            TotalMegaWatts = "100",
            Fuel = new MISOFuelList {
                Type = new List<MISOFuelItem> {
                    new MISOFuelItem {
                        Category = "Battery Storage",
                        MegaWatts = "-50"
                    },
                    new MISOFuelItem {
                        Category = "Imports",
                        MegaWatts = "-25"
                    },
                    new MISOFuelItem {
                        Category = "Wind",
                        MegaWatts = "175"
                    }
                }
            }

        };

        client.Setup(x => x.GetFuelMixSnapshotAsync(It.IsAny<CancellationToken>())).ReturnsAsync(response);

        var service = new MISOFuelMixIngestionService(client.Object, dbContext, Mock.Of<ILogger<MISOFuelMixIngestionService>>());

        await service.IngestAsync(CancellationToken.None);

        var snapshot = await dbContext.FuelMixSnapshots.Include(x => x.FuelMixElements).SingleAsync();

        snapshot.TotalMegaWatts.Should().Be(100);
        snapshot.FuelMixElements.Should().HaveCount(3);
        snapshot.FuelMixElements.Should().ContainSingle(x => x.Category == "Battery Storage" && x.MegaWatts == -50);
        snapshot.FuelMixElements.Should().ContainSingle(x => x.Category == "Imports" && x.MegaWatts == -25);
        snapshot.FuelMixElements.Should().ContainSingle(x => x.Category == "Wind" && x.MegaWatts == 175);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("DataSource=:memory:").Options;

        var context = new AppDbContext(options);

        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }
}
