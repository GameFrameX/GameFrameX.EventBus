using GameFrameX.Shashlik.EventBus.Abstractions;
using MongoDB.Driver;

namespace GameFrameX.Shashlik.EventBus.Storage.MongoDb;

public class MongoDbTransactionContext : ITransactionContext
{
    public MongoDbTransactionContext(IClientSessionHandle clientSessionHandle)
    {
        ClientSessionHandle = clientSessionHandle;
    }

    public IClientSessionHandle ClientSessionHandle { get; }

    public bool IsDone()
    {
        try
        {
            return !ClientSessionHandle.IsInTransaction;
        }
        catch
        {
            return true;
        }
    }
}