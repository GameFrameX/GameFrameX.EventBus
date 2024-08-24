using System.Threading;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace GameFrameX.Shashlik.EventBus.Storage.MemoryStorage;

public class MemoryStorageInitializer : IMessageStorageInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}