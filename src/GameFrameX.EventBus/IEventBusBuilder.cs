using Microsoft.Extensions.DependencyInjection;

namespace GameFrameX.EventBus
{
    public interface IEventBusBuilder
    {
        IServiceCollection Services { get; }
    }
}