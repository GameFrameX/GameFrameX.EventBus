using System.Data;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Shashlik.EventBus.RelationDbStorage;

namespace Shashlik.EventBus.Storage.MySql;

public class MySqlMessageStorage : RelationDbMessageStorageBase
{
    public MySqlMessageStorage(IOptionsMonitor<EventBusMySqlOptions> options, IConnectionString connectionString)
    {
        Options          = options;
        ConnectionString = connectionString;
    }

    private IOptionsMonitor<EventBusMySqlOptions> Options          { get; }
    private IConnectionString                     ConnectionString { get; }

    protected override string PublishedTableName
    {
        get { return Options.CurrentValue.PublishedTableName; }
    }

    protected override string ReceivedTableName
    {
        get { return Options.CurrentValue.ReceivedTableName; }
    }

    protected override string SqlTagCharPrefix
    {
        get { return "`"; }
    }

    protected override string SqlTagCharSuffix
    {
        get { return "`"; }
    }

    protected override string ReturnInsertIdSql
    {
        get
        {
            return @";
SELECT LAST_INSERT_ID()";
        }
    }


    protected override IDbConnection CreateConnection()
    {
        return new MySqlConnection(ConnectionString.ConnectionString);
    }
}