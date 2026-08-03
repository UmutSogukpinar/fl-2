using FantasyLeague.Application.Services.Drafts;
using FantasyLeague.Application.Services.Leagues;
using FantasyLeague.WebApi.Hubs;
using FantasyLeague.WebApi.Jobs.Drafts;
using FantasyLeague.WebApi.Jobs.Matches;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace FantasyLeague.WebApi.IntegrationTests.Jobs;

public sealed class HangfireJobIntegrationTests
{
    // Case: Draft scheduler job is enqueued through Hangfire
    // Reasoning: The Hangfire server should resolve the job and its dependencies from the application service provider.
    // Expected Result: Both scheduled draft operations are executed once.
    [Fact]
    public async Task DraftSchedulerJob_WhenEnqueued_ExecutesDraftOperations()
    {
        var completed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var draftService = new Mock<IDraftService>();
        draftService
            .Setup(service => service.StartDueDraftsAsync(
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        draftService
            .Setup(service => service.AutoPickExpiredAsync(
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => completed.TrySetResult())
            .ReturnsAsync([]);

        using var host = CreateHost(services =>
        {
            services.AddSingleton(draftService.Object);
            services.AddSingleton(Mock.Of<IHubContext<FantasyLeagueHub>>());
            services.AddScoped<DraftSchedulerJob>();
        });
        await host.StartAsync();

        var client = host.Services.GetRequiredService<IBackgroundJobClient>();
        client.Enqueue<DraftSchedulerJob>(
            job => job.ExecuteAsync(CancellationToken.None));

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        draftService.Verify(service => service.StartDueDraftsAsync(
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        draftService.Verify(service => service.AutoPickExpiredAsync(
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Case: Match scheduler job is enqueued through Hangfire
    // Reasoning: The Hangfire server should deserialize and execute the queued match job using dependency injection.
    // Expected Result: Due fixtures are processed once.
    [Fact]
    public async Task MatchSchedulerJob_WhenEnqueued_ProcessesDueFixtures()
    {
        var completed = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var leagueService = new Mock<ILeagueService>();
        leagueService
            .Setup(service => service.ProcessDueFixturesAsync(
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback(() => completed.TrySetResult())
            .ReturnsAsync(1);

        using var host = CreateHost(services =>
        {
            services.AddSingleton(leagueService.Object);
            services.AddScoped<MatchSchedulerJob>();
        });
        await host.StartAsync();

        var client = host.Services.GetRequiredService<IBackgroundJobClient>();
        client.Enqueue<MatchSchedulerJob>(
            job => job.ExecuteAsync(CancellationToken.None));

        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        leagueService.Verify(service => service.ProcessDueFixturesAsync(
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IHost CreateHost(Action<IServiceCollection> configureServices)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
                services.AddHangfire(configuration => configuration
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseInMemoryStorage());
                services.AddHangfireServer(options => options.WorkerCount = 1);
                configureServices(services);
            })
            .Build();
    }
}
