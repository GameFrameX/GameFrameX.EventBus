using System.Data;
using Microsoft.Extensions.Options;
using Npgsql;
using Shashlik.EventBus.RelationDbStorage;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.Storage.PostgreSQL;

public class PostgreSQLMessageStorage : RelationDbMessageStorageBase
{
    public PostgreSQLMessageStorage(IOptionsMonitor<EventBusPostgreSQLOptions> options,
        IConnectionString                                                      connectionString)
    {
        Options          = options;
        ConnectionString = connectionString;
    }

    private IOptionsMonitor<EventBusPostgreSQLOptions> Options          { get; }
    private IConnectionString                          ConnectionString { get; }

    protected override string PublishedTableName
    {
        get { return Options.CurrentValue.FullPublishedTableName; }
    }

    protected override string ReceivedTableName
    {
        get { return Options.CurrentValue.FullReceivedTableName; }
    }

    protected override string ReturnInsertIdSql
    {
        get { return " RETURNING id"; }
    }

    protected override string SqlTagCharPrefix
    {
        get { return "\""; }
    }

    protected override string SqlTagCharSuffix
    {
        get { return "\""; }
    }

    protected override string BoolTrueValue
    {
        get { return "true"; }
    }

    protected override string BoolFalseValue
    {
        get { return "false"; }
    }


    protected override IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString.ConnectionString);
    }

    protected override object ToSaveObject(MessageStorageModel model)
    {
        return new
        {
            model.Id,
            model.MsgId,
            model.Environment,
            CreateTime = model.CreateTime.GetLongDate(),
            DelayAt    = model.DelayAt?.GetLongDate() ?? 0,
            ExpireTime = model.ExpireTime?.GetLongDate() ?? 0,
            model.EventHandlerName,
            model.EventName,
            model.EventBody,
            model.EventItems,
            model.RetryCount,
            model.Status,
            model.IsLocking,
            LockEnd = model.LockEnd?.GetLongDate() ?? 0,
            IsDelay = model.DelayAt.HasValue,
        };
    }
}