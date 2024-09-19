﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Shashlik.EventBus;

namespace GameFrameX.EventBus.Storage.MongoDb;

public class MongoDbMessageStorageInitializer : IMessageStorageInitializer
{
    public MongoDbMessageStorageInitializer(IOptionsMonitor<EventBusMongoDbOptions> options,
        IConnection                                                                 connection)
    {
        Options = options;
        Client  = connection.Client;
    }

    private IOptionsMonitor<EventBusMongoDbOptions> Options { get; }
    private IMongoClient                            Client  { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var mongoDatabase = Client.GetDatabase(Options.CurrentValue.DataBase);
        var collections   = await mongoDatabase.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        var names         = await collections.ToListAsync(cancellationToken);
        if (names.All(r => r != Options.CurrentValue.PublishedCollectionName))
        {
            await mongoDatabase.CreateCollectionAsync(Options.CurrentValue.PublishedCollectionName,
                                                      cancellationToken: cancellationToken);
            var mongoCollection =
                mongoDatabase.GetCollection<MessageStorageModel>(Options.CurrentValue.PublishedCollectionName);
            await mongoCollection.Indexes.CreateManyAsync(new CreateIndexModel<MessageStorageModel>[]
                                                          {
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.MsgId))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.MsgId),
                                                                      Background = true,
                                                                      Unique     = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.EventName))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.EventName),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.CreateTime))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.CreateTime),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.ExpireTime))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.ExpireTime),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.Status))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.Status),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.LockEnd))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.LockEnd),
                                                                      Background = true,
                                                                  }),
                                                          },
                                                          cancellationToken);
        }

        if (names.All(r => r != Options.CurrentValue.ReceivedCollectionName))
        {
            await mongoDatabase.CreateCollectionAsync(Options.CurrentValue.ReceivedCollectionName,
                                                      cancellationToken: cancellationToken);
            var mongoCollection =
                mongoDatabase.GetCollection<MessageStorageModel>(Options.CurrentValue.ReceivedCollectionName);
            await mongoCollection.Indexes.CreateManyAsync(new CreateIndexModel<MessageStorageModel>[]
                                                          {
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.MsgId))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.MsgId),
                                                                      Background = true,
                                                                      Unique     = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.EventName))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.EventName),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.EventHandlerName))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.EventHandlerName),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.CreateTime))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.CreateTime),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.ExpireTime))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.ExpireTime),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.DelayAt))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.DelayAt),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.Status))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.Status),
                                                                      Background = true,
                                                                  }),
                                                              new(
                                                                  Builders<MessageStorageModel>.IndexKeys
                                                                                               .Descending(nameof(MessageStorageModel.LockEnd))
                                                                  , new CreateIndexOptions
                                                                  {
                                                                      Name       = nameof(MessageStorageModel.LockEnd),
                                                                      Background = true,
                                                                  }),
                                                          },
                                                          cancellationToken);
        }
    }
}