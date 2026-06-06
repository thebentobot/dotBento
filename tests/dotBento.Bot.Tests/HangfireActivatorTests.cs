using dotBento.Bot;
using Microsoft.Extensions.DependencyInjection;

namespace dotBento.Bot.Tests;

public sealed class HangfireActivatorTests
{
    private sealed class Job;

    [Fact]
    public void ActivateJob_ResolvesRegisteredJob()
    {
        var services = new ServiceCollection()
            .AddSingleton<Job>()
            .BuildServiceProvider();
        var activator = new HangfireActivator(services);

        var job = activator.ActivateJob(typeof(Job));

        Assert.IsType<Job>(job);
    }

    [Fact]
    public void ActivateJob_ThrowsForMissingJob()
    {
        var activator = new HangfireActivator(new ServiceCollection().BuildServiceProvider());

        Assert.Throws<InvalidOperationException>(() => activator.ActivateJob(typeof(Job)));
    }
}
