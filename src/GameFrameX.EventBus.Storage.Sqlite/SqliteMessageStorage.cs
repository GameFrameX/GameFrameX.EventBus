﻿using System.Data;
using GameFrameX.EventBus.RelationDbStorage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace GameFrameX.EventBus.Storage.Sqlite;

public class SqliteMessageStorage : RelationDbMessageStorageBase
{
    public SqliteMessageStorage(IOptionsMonitor<EventBusSqliteOptions> options, IConnectionString connectionString)
    {
        Options          = options;
        ConnectionString = connectionString;
    }

    private IOptionsMonitor<EventBusSqliteOptions> Options          { get; }
    private IConnectionString                      ConnectionString { get; }

    protected override string SqlTagCharPrefix
    {
        get { return "`"; }
    }

    protected override string SqlTagCharSuffix
    {
        get { return "`"; }
    }

    protected override string PublishedTableName
    {
        get { return Options.CurrentValue.PublishedTableName; }
    }

    protected override string ReceivedTableName
    {
        get { return Options.CurrentValue.ReceivedTableName; }
    }

    protected override string ReturnInsertIdSql
    {
        get
        {
            return @";
SELECT last_insert_rowid()";
        }
    }

    protected override IDbConnection CreateConnection()
    {
        return new SqliteConnection(ConnectionString.ConnectionString);
    }
}