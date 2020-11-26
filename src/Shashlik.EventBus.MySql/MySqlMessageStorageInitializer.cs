﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace Shashlik.EventBus.MySql
{
    public class MySqlMessageStorageInitializer : IMessageStorageInitializer
    {
        public MySqlMessageStorageInitializer(IOptionsMonitor<EventBusMySqlOptions> options,
            IConnectionString connectionString)
        {
            Options = options;
            ConnectionString = connectionString;
        }

        private IOptionsMonitor<EventBusMySqlOptions> Options { get; }
        private IConnectionString ConnectionString { get; }

        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            var sql = $@"
CREATE TABLE IF NOT EXISTS `{Options.CurrentValue.PublishTableName}`
(
	`msgId` VARCHAR(32) PRIMARY KEY,
	`environment` VARCHAR(32),
	`eventName` VARCHAR(255) NOT NULL,
	`eventBody` LONGTEXT NOT NULL,
	`createTime` BIGINT NOT NULL,
	`delayAt` BIGINT NOT NULL,
	`expireTime` BIGINT NOT NULL,
	`eventItems` LONGTEXT NULL,
	`status` VARCHAR(32) NOT NULL,
	`retryCount` INT NOT NULL,
	`isLocking` TINYINT NOT NULL,
	`lockEnd` LONG NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `{Options.CurrentValue.ReceiveTableName}`
(
	`msgId` VARCHAR(32) PRIMARY KEY,
	`environment` VARCHAR(32),
	`eventName` VARCHAR(255) NOT NULL,
	`eventHandlerName` VARCHAR(255) NOT NULL,
	`eventBody` LONGTEXT NOT NULL,
	`createTime` BIGINT NOT NULL,
	`isDelay` TINYINT NOT NULL,
	`delayAt` BIGINT NOT NULL,
	`expireTime` BIGINT NOT NULL,
	`eventItems` LONGTEXT NULL,
	`status` VARCHAR(32) NOT NULL,
	`retryCount` INT NOT NULL,
	`isLocking` TINYINT NOT NULL,
	`lockEnd` LONG NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
";

            await using var connection = new MySqlConnection(ConnectionString.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}