using Microsoft.Extensions.DependencyInjection;
using Shashlik.EventBus;

namespace GameFrameX.Shashlik.EventBus.Storage.MemoryStorage;

public static class MemoryStorageExtensions
{
    public static IEventBusBuilder AddMemoryStorage(this IEventBusBuilder service)
    {
        service.Services.AddSingleton<IMessageStorage, MemoryMessageStorage>();
        service.Services.AddTransient<IMessageStorageInitializer, MemoryStorageInitializer>();

        return service;
    }
}