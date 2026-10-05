using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MISOQueryingApp.Services;
using Moq;

namespace MISOQueryingApp.Tests;

public class MISOApiPollingTests
{
    [Fact]
    public async Task StartAsync_ShouldCallIngestionService()
    {
        var ingestionService = new Mock<IMISOFuelMixIngestionService>();

        ingestionService
            .Setup(x => x.IngestAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var scopeFactory = CreateScopeFactory(ingestionService.Object);

        var service = new MISOApiPollingService(scopeFactory, Mock.Of<ILogger<MISOApiPollingService>>(), TimeSpan.FromMilliseconds(100));

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await service.StopAsync(CancellationToken.None);

        ingestionService.Verify(x => x.IngestAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenIngestionFails_ShouldNotThrow()
    {
        var ingestionService = new Mock<IMISOFuelMixIngestionService>();

        ingestionService
            .Setup(x => x.IngestAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MISO API unavailable"));

        var scopeFactory = CreateScopeFactory(ingestionService.Object);

        var service = new MISOApiPollingService(scopeFactory, Mock.Of<ILogger<MISOApiPollingService>>(), TimeSpan.FromMilliseconds(100));

        var act = () => service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_WhenIngestionFails_ShouldLogError()
    {
        var exception = new InvalidOperationException("MISO API unavailable");

        var ingestionService = new Mock<IMISOFuelMixIngestionService>();

        ingestionService
            .Setup(x => x.IngestAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var logger = new Mock<ILogger<MISOApiPollingService>>();

        var scopeFactory = CreateScopeFactory(ingestionService.Object);

        var service = new MISOApiPollingService(scopeFactory, logger.Object, TimeSpan.FromMilliseconds(100));

        await service.StartAsync(CancellationToken.None);

        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => state.ToString()!.Contains("Fuel mix ingestion failed")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task StartAsync_ShouldResolveIngestionServiceFromScope()
    {
        var ingestionService = new Mock<IMISOFuelMixIngestionService>();

        ingestionService
            .Setup(x => x.IngestAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var scopeFactory = CreateScopeFactory(ingestionService.Object);

        var service = new MISOApiPollingService(scopeFactory, Mock.Of<ILogger<MISOApiPollingService>>(), TimeSpan.FromMilliseconds(100));

        await service.StartAsync(CancellationToken.None);

        ingestionService.Verify(x => x.IngestAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IServiceScopeFactory CreateScopeFactory(IMISOFuelMixIngestionService ingestionService)
    {
        var serviceProvider = new Mock<IServiceProvider>();

        serviceProvider
            .Setup(x => x.GetService(typeof(IMISOFuelMixIngestionService)))
            .Returns(ingestionService);

        var scope = new Mock<IServiceScope>();

        scope.SetupGet(x => x.ServiceProvider).Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();

        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        return scopeFactory.Object;
    }
}
