using MongoDB.Driver;
using Shashlik.EventBus.Abstractions;

namespace Shashlik.EventBus.MongoDb;

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