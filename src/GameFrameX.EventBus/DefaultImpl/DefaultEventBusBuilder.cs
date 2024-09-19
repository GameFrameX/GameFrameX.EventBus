using Microsoft.Extensions.DependencyInjection;

namespace GameFrameX.EventBus.DefaultImpl
{
    internal class DefaultEventBusBuilder : IEventBusBuilder
    {
        public DefaultEventBusBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}