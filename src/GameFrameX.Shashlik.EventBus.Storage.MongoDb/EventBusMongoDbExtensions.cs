﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Shashlik.EventBus.Abstractions;
using Shashlik.EventBus.Utils;

namespace Shashlik.EventBus.MongoDb;

public static class EventBusMongoDbExtensions
{
    /// <summary>
    ///     使用连接字符串初始化注册MongoDb存储
    /// </summary>
    /// <param name="eventBusBuilder"></param>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="publishCollectionName">已发布消息集合名称，默认eventbus_published</param>
    /// <param name="receiveCollectionName">已接收消息集合名称，默认eventbus_received</param>
    /// <returns></returns>
    public static IEventBusBuilder AddMongoDb(this IEventBusBuilder eventBusBuilder, string connectionString, string publishCollectionName = null, string receiveCollectionName = null)
    {
        eventBusBuilder.Services.Configure<EventBusMongoDbOptions>(options =>
                                                                   {
                                                                       options.ConnectionString = connectionString;
                                                                       if (!publishCollectionName.IsNullOrWhiteSpace())
                                                                       {
                                                                           options.PublishedCollectionName = publishCollectionName!;
                                                                       }

                                                                       if (!receiveCollectionName.IsNullOrWhiteSpace())
                                                                       {
                                                                           options.ReceivedCollectionName = receiveCollectionName!;
                                                                       }
                                                                   });

        return eventBusBuilder.AddMongoDb();
    }

    /// <summary>
    ///     从配置初中读取MongoDb连接数据
    /// </summary>
    /// <param name="eventBusBuilder"></param>
    /// <param name="configurationSection">配置节点</param>
    /// <returns></returns>
    public static IEventBusBuilder AddMongoDb(this IEventBusBuilder eventBusBuilder, IConfigurationSection configurationSection)
    {
        eventBusBuilder.Services.Configure<EventBusMongoDbOptions>(configurationSection);
        return eventBusBuilder.AddMongoDb();
    }

    /// <summary>
    ///     使用MongoDb存储
    /// </summary>
    /// <param name="eventBusBuilder"></param>
    /// <param name="optionsAction"></param>
    /// <returns></returns>
    public static IEventBusBuilder AddMongoDb(this IEventBusBuilder eventBusBuilder, Action<EventBusMongoDbOptions> optionsAction = null)
    {
        eventBusBuilder.Services.AddOptions<EventBusMongoDbOptions>();
        if (optionsAction != null)
        {
            eventBusBuilder.Services.Configure(optionsAction);
        }

        eventBusBuilder.Services.AddSingleton<IMessageStorage, MongoDbMessageStorage>();
        eventBusBuilder.Services.AddTransient<IMessageStorageInitializer, MongoDbMessageStorageInitializer>();
        eventBusBuilder.Services.AddSingleton<IConnection, DefaultConnection>();

        return eventBusBuilder;
    }

    /// <summary>
    ///     获取事务上下文
    /// </summary>
    /// <param name="clientSessionHandle"></param>
    /// <returns></returns>
    public static ITransactionContext GetTransactionContext(this IClientSessionHandle clientSessionHandle)
    {
        return new MongoDbTransactionContext(clientSessionHandle);
    }
}