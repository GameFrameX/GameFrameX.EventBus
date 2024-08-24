using System.Data;
using GameFrameX.Shashlik.EventBus.Abstractions;

// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace GameFrameX.Shashlik.EventBus.RelationDbStorage;

public class RelationDbStorageTransactionContext : ITransactionContext
{
    public RelationDbStorageTransactionContext(IDbTransaction dbTransaction)
    {
        DbTransaction = dbTransaction;
    }

    public IDbTransaction DbTransaction { get; }

    public virtual bool IsDone()
    {
        try
        {
            return DbTransaction.Connection is null;
        }
        catch
        {
            return true;
        }
    }
}