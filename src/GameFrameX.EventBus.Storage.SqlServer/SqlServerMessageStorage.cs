using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.EventBus.RelationDbStorage;
using GameFrameX.EventBus.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;

namespace GameFrameX.EventBus.Storage.SqlServer;

public class SqlServerMessageStorage : RelationDbMessageStorageBase
{
    public SqlServerMessageStorage(IOptionsMonitor<EventBusSqlServerOptions> options,
        IConnectionString                                                    connectionString)
    {
        Options          = options;
        ConnectionString = connectionString;
    }

    private IOptionsMonitor<EventBusSqlServerOptions> Options          { get; }
    private IConnectionString                         ConnectionString { get; }

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
        get
        {
            return @";
SELECT SCOPE_IDENTITY()";
        }
    }

    protected override string SqlTagCharPrefix
    {
        get { return "["; }
    }

    protected override string SqlTagCharSuffix
    {
        get { return "]"; }
    }


    public override async ValueTask<bool> IsCommittedAsync(string messageId,
        CancellationToken                                         cancellationToken = default)
    {
        var sql   = $"SELECT TOP 1 1 FROM {Options.CurrentValue.FullPublishedTableName} WHERE [msgId] = @msgId;";
        var count = await ScalarAsync<int>(sql, new { msgId = messageId, }, cancellationToken);
        return count > 0;
    }


    public override async Task<List<MessageStorageModel>> SearchPublishedAsync(string eventName, MessageStatus status, int skip, int take, CancellationToken cancellationToken)
    {
        var where = new StringBuilder();
        if (!eventName.IsNullOrWhiteSpace())
        {
            where.Append(" AND [eventName] = @eventName");
        }

        if (status != MessageStatus.None)
        {
            where.Append(" AND [status] = @status");
        }

        var sql = $@"
SELECT * FROM 
(
    SELECT *, ROW_NUMBER() OVER(ORDER BY [createTime] DESC) AS rowNumber FROM {Options.CurrentValue.FullPublishedTableName}
    WHERE 
        1 = 1{where}
) AS A
WHERE A.[rowNumber] BETWEEN {skip} and {skip + take}
ORDER BY A.[createTime] DESC;
";

        return await QueryModelAsync(sql, new { eventName, status, }, cancellationToken)
                   .ConfigureAwait(false);
    }

    public override async Task<List<MessageStorageModel>> SearchReceivedAsync(string eventName, string eventHandlerName, MessageStatus status, int skip, int take, CancellationToken cancellationToken)
    {
        var where = new StringBuilder();
        if (!eventName.IsNullOrWhiteSpace())
        {
            where.Append(" AND [eventName] = @eventName");
        }

        if (!eventHandlerName.IsNullOrWhiteSpace())
        {
            where.Append(" AND [eventHandlerName] = @eventHandlerName");
        }

        if (status != MessageStatus.None)
        {
            where.Append(" AND [status] = @status");
        }

        var sql = $@"
SELECT * FROM 
(
    SELECT *,ROW_NUMBER() OVER(ORDER BY [createTime] DESC) AS rowNumber FROM {Options.CurrentValue.FullReceivedTableName}
    WHERE 
        1 = 1{where}
) AS A
WHERE A.[rowNumber] BETWEEN {skip} and {skip + take}
ORDER BY A.[createTime] DESC;
";

        return await QueryModelAsync(sql, new { eventName, eventHandlerName, status, }, cancellationToken)
                   .ConfigureAwait(false);
    }

    public override async Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAsync(
        int               count,
        int               delayRetrySecond,
        int               maxFailedRetryCount,
        string            environment,
        CancellationToken cancellationToken = default)
    {
        var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond).GetLongDate();
        var now             = DateTimeOffset.Now;
        var nowLong         = now.GetLongDate();

        var sql = $@"
SELECT  TOP {count} * FROM {Options.CurrentValue.FullPublishedTableName}
WHERE
    [environment] = '{environment}'
    AND [createTime] < {createTimeLimit}
    AND [retryCount] < {maxFailedRetryCount}
    AND ([isLocking] = 0 OR [lockEnd] < {nowLong})
    AND ([status] = '{MessageStatus.Scheduled}' OR [status] = '{MessageStatus.Failed}')
;
";

        return await QueryModelAsync(sql, null, cancellationToken).ConfigureAwait(false);
    }

    public override async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(
        int               count,
        int               delayRetrySecond,
        int               maxFailedRetryCount,
        string            environment,
        CancellationToken cancellationToken = default)
    {
        var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond).GetLongDate();
        var now             = DateTimeOffset.Now;
        var nowLong         = now.GetLongDate();

        var sql = $@"
SELECT TOP {count} * FROM {Options.CurrentValue.FullReceivedTableName}
WHERE
    [environment] = '{environment}'
    AND (([isDelay] = 0 AND [createTime] < {createTimeLimit}) OR ([isDelay] = 1 AND [delayAt] <= {nowLong} ))
    AND [retryCount] < {maxFailedRetryCount}
    AND ([isLocking] = 0 OR [lockEnd] < {nowLong})
    AND ([status] = '{MessageStatus.Scheduled}' OR [status] = '{MessageStatus.Failed}')
;
";

        return await QueryModelAsync(sql, null, cancellationToken).ConfigureAwait(false);
    }


    protected override IDbConnection CreateConnection()
    {
        return new SqlConnection(ConnectionString.ConnectionString);
    }
}