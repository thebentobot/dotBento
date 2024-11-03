using Hangfire;

namespace dotBento.Bot;

public sealed class HangfireActivator(IServiceProvider serviceProvider) : JobActivator
{
    public override object ActivateJob(Type type)
    {
        return serviceProvider.GetService(type) ?? throw new InvalidOperationException();
    }
}