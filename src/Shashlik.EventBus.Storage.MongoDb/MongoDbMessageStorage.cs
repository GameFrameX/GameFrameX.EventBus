﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Shashlik.EventBus.Abstractions;
using Shashlik.EventBus.Utils;

// ReSharper disable ConvertIfStatementToSwitchExpression
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable RedundantIfElseBlock

namespace Shashlik.EventBus.MongoDb;

public class MongoDbMessageStorage : IMessageStorage
{
    public MongoDbMessageStorage(IOptionsMonitor<EventBusMongoDbOptions> options, IConnection connection)
    {
        Options    = options;
        Connection = connection;
    }

    private IOptionsMonitor<EventBusMongoDbOptions> Options    { get; }
    private IConnection                             Connection { get; }

    public async ValueTask<bool> IsCommittedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var mongoCollection = GetPublishedCollection();
        var msg             = await mongoCollection.FindAsync(r => r.MsgId == messageId, cancellationToken: cancellationToken);
        return await msg.AnyAsync(cancellationToken);
    }

    public async Task<MessageStorageModel> FindPublishedByMsgIdAsync(string messageId, CancellationToken cancellationToken)
    {
        var mongoCollection = GetPublishedCollection();
        var msg             = await mongoCollection.FindAsync(r => r.MsgId == messageId, cancellationToken: cancellationToken);
        return await msg.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MessageStorageModel> FindPublishedByIdAsync(string storageId, CancellationToken cancellationToken)
    {
        var mongoCollection = GetPublishedCollection();
        var msg             = await mongoCollection.FindAsync(r => r.Id == storageId, cancellationToken: cancellationToken);
        return await msg.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MessageStorageModel> FindReceivedByMsgIdAsync(string messageId, EventHandlerDescriptor eventHandlerDescriptor, CancellationToken cancellationToken = default)
    {
        var mongoCollection = GetReceivedCollection();
        var msg = await mongoCollection.FindAsync(
                      r => r.MsgId == messageId && r.EventHandlerName == eventHandlerDescriptor.EventHandlerName,
                      cancellationToken: cancellationToken);
        return await msg.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MessageStorageModel> FindReceivedByIdAsync(string storageId, CancellationToken cancellationToken)
    {
        var mongoCollection = GetReceivedCollection();
        var msg             = await mongoCollection.FindAsync(r => r.Id == storageId, cancellationToken: cancellationToken);
        return await msg.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<List<MessageStorageModel>> SearchPublishedAsync(string eventName, MessageStatus status, int skip, int take, CancellationToken cancellationToken)
    {
        var mongoCollection = GetPublishedCollection();
        var builder         = Builders<MessageStorageModel>.Filter;
        var filter          = builder.Empty;
        if (!eventName.IsNullOrWhiteSpace())
        {
            filter = builder.And(builder.Eq(r => r.EventName, eventName));
        }

        if (status != MessageStatus.None)
        {
            filter = builder.And(builder.Eq(r => r.Status, status));
        }

        return await mongoCollection.Find(filter)
                                    .SortByDescending(r => r.CreateTime)
                                    .Skip(skip).Limit(take)
                                    .ToListAsync(cancellationToken);
    }

    public async Task<List<MessageStorageModel>> SearchReceivedAsync(string eventName, string eventHandlerName, MessageStatus status, int skip, int take, CancellationToken cancellationToken)
    {
        var mongoCollection = GetReceivedCollection();
        var builder         = Builders<MessageStorageModel>.Filter;
        var filter          = builder.Empty;
        if (!eventName.IsNullOrWhiteSpace())
        {
            filter = builder.And(builder.Eq(r => r.EventName, eventName));
        }

        if (!eventHandlerName.IsNullOrWhiteSpace())
        {
            filter = builder.And(builder.Eq(r => r.EventHandlerName, eventHandlerName));
        }

        if (status != MessageStatus.None)
        {
            filter = builder.And(builder.Eq(r => r.Status, status));
        }

        return await mongoCollection.Find(filter)
                                    .SortByDescending(r => r.CreateTime)
                                    .Skip(skip).Limit(take)
                                    .ToListAsync(cancellationToken);
    }

    public async Task<string> SavePublishedAsync(MessageStorageModel message, ITransactionContext transactionContext, CancellationToken cancellationToken = default)
    {
        IMongoCollection<MessageStorageModel> mongoCollection;
        MongoDbTransactionContext             mongoDbTransactionContext = null;
        if (transactionContext is null)
        {
            mongoCollection = GetPublishedCollection();
        }
        else
        {
            if (transactionContext is MongoDbTransactionContext mongoDbTransactionContext1)
            {
                mongoDbTransactionContext = mongoDbTransactionContext1;
                mongoCollection = mongoDbTransactionContext.ClientSessionHandle.Client
                                                           .GetDatabase(Options.CurrentValue.DataBase)
                                                           .GetCollection<MessageStorageModel>(Options.CurrentValue.PublishedCollectionName);
            }
            else
            {
                throw new InvalidCastException(
                    $"[EventBus-MongoDb]Storage only support transaction context of {typeof(MongoDbTransactionContext)}");
            }
        }

        message.Id = ObjectId.GenerateNewId().ToString();

        if (mongoDbTransactionContext is null)
        {
            await mongoCollection.InsertOneAsync(message, new InsertOneOptions(), cancellationToken);
        }
        else
        {
            await mongoCollection.InsertOneAsync(mongoDbTransactionContext.ClientSessionHandle,
                                                 message, new InsertOneOptions(), cancellationToken);
        }

        return message.Id;
    }

    public async Task<string> SaveReceivedAsync(MessageStorageModel message, CancellationToken cancellationToken = default)
    {
        var mongoCollection = GetReceivedCollection();
        message.Id = ObjectId.GenerateNewId().ToString();
        await mongoCollection.InsertOneAsync(message, new InsertOneOptions(), cancellationToken);
        return message.Id;
    }

    public async Task UpdatePublishedAsync(string storageId, MessageStatus status, int retryCount, DateTimeOffset? expireTime, CancellationToken cancellationToken = default)
    {
        var mongoCollection = GetPublishedCollection();

        await mongoCollection.FindOneAndUpdateAsync(r => r.Id == storageId,
                                                    Builders<MessageStorageModel>.Update
                                                                                 .Set(r => r.Status, status)
                                                                                 .Set(r => r.RetryCount, retryCount)
                                                                                 .Set(r => r.ExpireTime, expireTime),
                                                    cancellationToken: cancellationToken);
    }

    public async Task UpdateReceivedAsync(string storageId, MessageStatus status, int retryCount, DateTimeOffset? expireTime, CancellationToken cancellationToken = default)
    {
        var mongoCollection = GetReceivedCollection();

        await mongoCollection.FindOneAndUpdateAsync(r => r.Id == storageId,
                                                    Builders<MessageStorageModel>.Update
                                                                                 .Set(r => r.Status, status)
                                                                                 .Set(r => r.RetryCount, retryCount)
                                                                                 .Set(r => r.ExpireTime, expireTime),
                                                    cancellationToken: cancellationToken);
    }

    public async Task<bool> TryLockPublishedAsync(string storageId, DateTimeOffset lockEndAt, CancellationToken cancellationToken)
    {
        if (lockEndAt <= DateTimeOffset.Now)
        {
            throw new ArgumentOutOfRangeException(nameof(lockEndAt));
        }

        var mongoCollection = GetPublishedCollection();

        var res = await mongoCollection.FindOneAndUpdateAsync(
                      r => r.Id == storageId && (!r.IsLocking || r.LockEnd < DateTimeOffset.Now),
                      Builders<MessageStorageModel>.Update
                                                   .Set(r => r.IsLocking, true)
                                                   .Set(r => r.LockEnd, lockEndAt)
                      , cancellationToken: cancellationToken);
        return res is not null;
    }

    public async Task<bool> TryLockReceivedAsync(string storageId, DateTimeOffset lockEndAt, CancellationToken cancellationToken)
    {
        if (lockEndAt <= DateTimeOffset.Now)
        {
            throw new ArgumentOutOfRangeException(nameof(lockEndAt));
        }

        var mongoCollection = GetReceivedCollection();

        var res = await mongoCollection.FindOneAndUpdateAsync(
                      r => r.Id == storageId && (!r.IsLocking || r.LockEnd < DateTimeOffset.Now),
                      Builders<MessageStorageModel>.Update
                                                   .Set(r => r.IsLocking, true)
                                                   .Set(r => r.LockEnd, lockEndAt)
                      , cancellationToken: cancellationToken);
        return res is not null;
    }

    public async Task DeleteExpiresAsync(CancellationToken cancellationToken = default)
    {
        var mongoCollection1 = GetReceivedCollection();
        await mongoCollection1.DeleteManyAsync(r =>
                                                   r.ExpireTime < DateTimeOffset.Now && r.Status == MessageStatus.Succeeded,
                                               cancellationToken);
        var mongoCollection2 = GetPublishedCollection();
        await mongoCollection2.DeleteManyAsync(r =>
                                                   r.ExpireTime < DateTimeOffset.Now && r.Status == MessageStatus.Succeeded,
                                               cancellationToken);
    }

    public async Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAsync(int count, int delayRetrySecond, int maxFailedRetryCount, string environment, CancellationToken cancellationToken = default)
    {
        var createTimeLimit  = DateTimeOffset.Now.AddSeconds(-delayRetrySecond);
        var now              = DateTimeOffset.Now;
        var mongoCollection1 = GetPublishedCollection();
        var res = await mongoCollection1.FindAsync(r =>
                                                       r.Environment == environment
                                                       && r.CreateTime < createTimeLimit
                                                       && r.RetryCount < maxFailedRetryCount
                                                       && (!r.IsLocking || r.LockEnd < now)
                                                       && (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed),
                                                   cancellationToken: cancellationToken);

        return await res.ToListAsync(cancellationToken);
    }

    public async Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(int count, int delayRetrySecond, int maxFailedRetryCount, string environment, CancellationToken cancellationToken = default)
    {
        var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond);
        var now             = DateTimeOffset.Now;
        var mongoCollection = GetReceivedCollection();
        var res = await mongoCollection.FindAsync(r =>
                                                      r.Environment == environment
                                                      && r.CreateTime < createTimeLimit
                                                      && r.RetryCount < maxFailedRetryCount
                                                      && (!r.IsLocking || r.LockEnd < now)
                                                      && (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed),
                                                  cancellationToken: cancellationToken);

        return await res.ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<MessageStatus, int>> GetPublishedMessageStatusCountsAsync(CancellationToken cancellationToken)
    {
        var mongoCollection = GetPublishedCollection();
        var res = await mongoCollection.Aggregate(new AggregateOptions
                                       {
                                           AllowDiskUse = true,
                                       })
                                       .Group(r => r.Status, r => new
                                       {
                                           r.Key,
                                           Count = r.Count(),
                                       }).ToListAsync(cancellationToken);

        return res.ToDictionary(r => r.Key, r => r.Count);
    }

    public async Task<Dictionary<MessageStatus, int>> GetReceivedMessageStatusCountAsync(CancellationToken cancellationToken)
    {
        var mongoCollection = GetReceivedCollection();
        var res = await mongoCollection.Aggregate(new AggregateOptions
                                       {
                                           AllowDiskUse = true,
                                       })
                                       .Group(r => r.Status, r => new
                                       {
                                           r.Key,
                                           Count = r.Count(),
                                       }).ToListAsync(cancellationToken);

        return res.ToDictionary(r => r.Key, r => r.Count);
    }

    private IMongoCollection<MessageStorageModel> GetPublishedCollection()
    {
        return Connection.Client.GetDatabase(Options.CurrentValue.DataBase).GetCollection<MessageStorageModel>(Options.CurrentValue.PublishedCollectionName);
    }

    private IMongoCollection<MessageStorageModel> GetReceivedCollection()
    {
        return Connection.Client.GetDatabase(Options.CurrentValue.DataBase).GetCollection<MessageStorageModel>(Options.CurrentValue.ReceivedCollectionName);
    }
}