﻿#nullable enable
using System;

namespace GameFrameX.EventBus.Storage.PostgreSQL;

public class EventBusPostgreSQLOptions
{
    /// <summary>
    ///     模式名
    ///     <para></para>
    ///     9.2及以下不支持CREATE SCHEMA IF NOT EXISTS，只能使用public
    /// </summary>
    public string Schema { get; set; } = "eventbus";

    /// <summary>
    ///     已发布消息表名
    /// </summary>
    public string PublishedTableName { get; set; } = "published";

    /// <summary>
    ///     已接收的消息表名
    /// </summary>
    public string ReceivedTableName { get; set; } = "received";

    /// <summary>
    ///     ef数据库上下文类型, 和<see cref="ConnectionString" />配其中一个，优先使用<see cref="DbContextType" />
    /// </summary>
    public Type? DbContextType { get; set; }

    /// <summary>
    ///     mysql数据库连接字符串，和<see cref="DbContextType" />配其中一个，优先使用<see cref="DbContextType" />
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    ///     已发布消息表全名
    /// </summary>
    public string FullPublishedTableName
    {
        get { return $@"""{Schema}"".""{PublishedTableName}"""; }
    }

    /// <summary>
    ///     已接收的消息表全名
    /// </summary>
    public string FullReceivedTableName
    {
        get { return $@"""{Schema}"".""{ReceivedTableName}"""; }
    }
}