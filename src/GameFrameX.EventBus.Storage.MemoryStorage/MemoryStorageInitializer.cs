using System.Threading;
using System.Threading.Tasks;

namespace GameFrameX.EventBus.Storage.MemoryStorage;

public class MemoryStorageInitializer : IMessageStorageInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}