using GameFrameX.EventBus.Abstractions;
using MongoDB.Driver;

namespace GameFrameX.EventBus.Storage.MongoDb;

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