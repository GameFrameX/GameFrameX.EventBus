﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.EventBus.Abstractions;
using GameFrameX.EventBus.Utils;
using Shashlik.EventBus;

// ReSharper disable ConvertIfStatementToSwitchExpression
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable RedundantIfElseBlock

namespace GameFrameX.EventBus.Storage.MemoryStorage;

public class MemoryMessageStorage : IMessageStorage
{
    private static          int                                               _lastId;
    private static readonly object                                            IdLck      = new();
    private static readonly object                                            DeleteLck  = new();
    private readonly        ConcurrentDictionary<string, MessageStorageModel> _published = new();
    private readonly        ConcurrentDictionary<string, MessageStorageModel> _received  = new();

    public ValueTask<bool> IsCommittedAsync(string messageId, CancellationToken cancellationToken)
    {
        return new ValueTask<bool>(_published.Any(r => r.Value.MsgId == messageId));
    }

    public async Task<MessageStorageModel> FindPublishedByMsgIdAsync(string messageId,
        CancellationToken                                                   cancellationToken)
    {
        var res = _published.Values.FirstOrDefault(r => r.MsgId == messageId);
        if (res != null)
        {
            res.EventHandlerName = null;
        }

        return await Task.FromResult(res);
    }

    public Task<MessageStorageModel> FindPublishedByIdAsync(string storageId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_published.GetOrDefault(storageId));
    }

    public async Task<MessageStorageModel> FindReceivedByMsgIdAsync(string messageId, EventHandlerDescriptor eventHandlerDescriptor, CancellationToken cancellationToken)
    {
        return await Task.FromResult(_received.Values.FirstOrDefault(r => r.MsgId == messageId && r.EventHandlerName == eventHandlerDescriptor.EventHandlerName));
    }

    public Task<MessageStorageModel> FindReceivedByIdAsync(string storageId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_received.GetOrDefault(storageId));
    }

    public Task<List<MessageStorageModel>> SearchPublishedAsync(string eventName, MessageStatus status, int skip, int take, CancellationToken cancellationToken)
    {
        var list = _published.Values
                             .WhereIf(!eventName.IsNullOrWhiteSpace(), r => r.EventName == eventName)
                             .WhereIf(status != MessageStatus.None, r => r.Status == status)
                             .Skip(skip)
                             .Take(take)
                             .ToList();
        return Task.FromResult(list);
    }

    public Task<List<MessageStorageModel>> SearchReceivedAsync(string eventName, string eventHandlerName, MessageStatus status, int skip, int take, CancellationToken cancellationToken)
    {
        var list = _received.Values
                            .WhereIf(!eventName.IsNullOrWhiteSpace(), r => r.EventName == eventName)
                            .WhereIf(!eventHandlerName.IsNullOrWhiteSpace(), r => r.EventHandlerName == eventHandlerName)
                            .WhereIf(status != MessageStatus.None, r => r.Status == status)
                            .Skip(skip)
                            .Take(take)
                            .ToList();
        return Task.FromResult(list);
    }

    public Task<string> SavePublishedAsync(MessageStorageModel message, ITransactionContext transactionContext, CancellationToken cancellationToken)
    {
        message.Id = AutoIncrementId();
        if (_published.TryAdd(message.Id, message))
        {
            return Task.FromResult(message.Id);
        }

        throw new Exception($"save published message error, msgId: {message.MsgId}");
    }

    public Task<string> SaveReceivedAsync(MessageStorageModel message, CancellationToken cancellationToken)
    {
        message.Id = AutoIncrementId();
        if (_received.TryAdd(message.Id, message))
        {
            return Task.FromResult(message.Id);
        }

        throw new Exception($"save received message error, msgId: {message.MsgId}");
    }

    public Task UpdatePublishedAsync(string storageId, MessageStatus status, int retryCount, DateTimeOffset? expireTime, CancellationToken cancellationToken)
    {
        if (_published.TryGetValue(storageId, out var model))
        {
            model.Status     = status;
            model.RetryCount = retryCount;
            model.ExpireTime = expireTime;
        }
        else
        {
            throw new InvalidOperationException();
        }

        return Task.CompletedTask;
    }

    public Task UpdateReceivedAsync(string storageId, MessageStatus status, int retryCount, DateTimeOffset? expireTime, CancellationToken cancellationToken)
    {
        if (_received.TryGetValue(storageId, out var model))
        {
            model.Status     = status;
            model.RetryCount = retryCount;
            model.ExpireTime = expireTime;
        }
        else
        {
            throw new InvalidOperationException();
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryLockPublishedAsync(string id, DateTimeOffset lockEndAt, CancellationToken cancellationToken)
    {
        if (lockEndAt <= DateTimeOffset.Now)
        {
            throw new ArgumentOutOfRangeException(nameof(lockEndAt));
        }

        if (_published.TryGetValue(id, out var model))
        {
            if (!model.IsLocking || DateTimeOffset.Now > model.LockEnd)
            {
                LockAndUpdate(model, _ =>
                                     {
                                         model.IsLocking = true;
                                         model.LockEnd   = lockEndAt;
                                     });
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        else
        {
            return Task.FromResult(false);
        }
    }

    public Task<bool> TryLockReceivedAsync(string storageId, DateTimeOffset lockEndAt, CancellationToken cancellationToken)
    {
        if (lockEndAt <= DateTimeOffset.Now)
        {
            throw new ArgumentOutOfRangeException(nameof(lockEndAt));
        }

        if (_received.TryGetValue(storageId, out var model))
        {
            if (!model.IsLocking || DateTimeOffset.Now > model.LockEnd)
            {
                LockAndUpdate(model, _ =>
                                     {
                                         model.IsLocking = true;
                                         model.LockEnd   = lockEndAt;
                                     });
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        else
        {
            return Task.FromResult(false);
        }
    }

    public Task DeleteExpiresAsync(CancellationToken cancellationToken)
    {
        DeleteExpires();
        return Task.CompletedTask;
    }

    public Task<List<MessageStorageModel>> GetPublishedMessagesOfNeedRetryAsync(int count, int delayRetrySecond, int maxFailedRetryCount, string environment, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetPublishedMessagesOfNeedRetryAndLock(count, delayRetrySecond, maxFailedRetryCount, environment));
    }

    public Task<List<MessageStorageModel>> GetReceivedMessagesOfNeedRetryAsync(int count, int delayRetrySecond,
        int                                                                        maxFailedRetryCount,
        string                                                                     environment,
        CancellationToken                                                          cancellationToken)
    {
        return Task.FromResult(
            GetReceivedMessagesOfNeedRetryAndLock(count, delayRetrySecond, maxFailedRetryCount, environment));
    }

    public Task<Dictionary<MessageStatus, int>> GetPublishedMessageStatusCountsAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_published.Values.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count()));
    }

    public Task<Dictionary<MessageStatus, int>> GetReceivedMessageStatusCountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_received.Values.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count()));
    }

    private static string AutoIncrementId()
    {
        lock (IdLck)
        {
            _lastId++;
        }

        return _lastId.ToString();
    }

    private void LockAndUpdate(MessageStorageModel model, Action<MessageStorageModel> update)
    {
        lock (this)
        {
            update(model);
        }
    }

    public void DeleteExpires()
    {
        lock (DeleteLck)
        {
            var items1 = _published
                         .Values
                         .Where(r => r.ExpireTime.HasValue && r.ExpireTime < DateTimeOffset.Now)
                         .Where(r => r.Status == MessageStatus.Succeeded)
                         .ToList();
            foreach (var item in items1)
            {
                _published.TryRemove(item.Id, out _);
            }

            var items2 = _received
                         .Values
                         .Where(r => r.ExpireTime.HasValue && r.ExpireTime < DateTimeOffset.Now)
                         .Where(r => r.Status == MessageStatus.Succeeded)
                         .ToList();
            foreach (var item in items2)
            {
                _received.TryRemove(item.Id, out _);
            }
        }
    }

    public List<MessageStorageModel> GetPublishedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond, int maxFailedRetryCount, string environment)
    {
        var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond);
        var now             = DateTimeOffset.Now;
        var res             = new List<MessageStorageModel>();
        var counter         = 0;
        foreach (var r in _published.Values)
        {
            if (counter > count)
            {
                return res;
            }

            if (r.Environment == environment
                && r.CreateTime < createTimeLimit
                && r.RetryCount < maxFailedRetryCount
                && (!r.IsLocking || r.LockEnd < now)
                && (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed))
            {
                res.Add(r);
                count++;
            }
        }

        return res;
    }

    public List<MessageStorageModel> GetReceivedMessagesOfNeedRetryAndLock(int count, int delayRetrySecond, int maxFailedRetryCount, string environment)
    {
        var createTimeLimit = DateTimeOffset.Now.AddSeconds(-delayRetrySecond);
        var now             = DateTimeOffset.Now;
        var res             = new List<MessageStorageModel>();
        foreach (var r in _received.Values)
        {
            if (r.Environment == environment
                && r.CreateTime < createTimeLimit
                && r.RetryCount < maxFailedRetryCount
                && (!r.IsLocking || r.LockEnd < now)
                && (r.Status == MessageStatus.Scheduled || r.Status == MessageStatus.Failed))
            {
                res.Add(r);
            }
        }

        return res;
    }
}