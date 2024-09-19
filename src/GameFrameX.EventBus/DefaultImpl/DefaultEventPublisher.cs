﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameFrameX.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shashlik.EventBus;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable MethodSupportsCancellation

namespace GameFrameX.EventBus.DefaultImpl
{
    public class DefaultEventPublisher : IEventPublisher
    {
        public DefaultEventPublisher(
            IMessageStorage                messageStorage,
            IMessageSerializer             messageSerializer,
            IEventNameRuler                eventNameRuler,
            IOptions<EventBusOptions>      options,
            IMessageIdGenerator            messageIdGenerator,
            IPublishHandler                publishHandler,
            ILogger<DefaultEventPublisher> logger)
        {
            MessageStorage     = messageStorage;
            MessageSerializer  = messageSerializer;
            EventNameRuler     = eventNameRuler;
            Options            = options;
            MessageIdGenerator = messageIdGenerator;
            PublishHandler     = publishHandler;
            Logger             = logger;
        }

        private IMessageStorage                MessageStorage     { get; }
        private IMessageSerializer             MessageSerializer  { get; }
        private IEventNameRuler                EventNameRuler     { get; }
        private IMessageIdGenerator            MessageIdGenerator { get; }
        private IOptions<EventBusOptions>      Options            { get; }
        private IPublishHandler                PublishHandler     { get; }
        private ILogger<DefaultEventPublisher> Logger             { get; }

        public async Task PublishAsync<TEvent>(TEvent @event, ITransactionContext transactionContext, IDictionary<string, string> additionalItems = null, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await InnerPublish(@event, null, transactionContext, additionalItems, cancellationToken).ConfigureAwait(false);
        }

        public async Task PublishAsync<TEvent>(TEvent @event, DateTimeOffset delayAt, ITransactionContext transactionContext, IDictionary<string, string> additionalItems = null, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await InnerPublish(@event, delayAt, transactionContext, additionalItems, cancellationToken).ConfigureAwait(false);
        }

        private async Task InnerPublish<TEvent>(TEvent @event, DateTimeOffset? delayAt, ITransactionContext transactionContext, IDictionary<string, string> additionalItems = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event, nameof(@event));

            var now       = DateTimeOffset.Now;
            var eventName = EventNameRuler.GetName(typeof(TEvent));
            var msgId     = MessageIdGenerator.GenerateId();
            additionalItems ??= new Dictionary<string, string>();
            additionalItems.Add(EventBusConsts.SendAtHeaderKey, now.ToString());
            additionalItems.Add(EventBusConsts.EventNameHeaderKey, eventName);
            additionalItems.Add(EventBusConsts.MsgIdHeaderKey, msgId);
            if (delayAt.HasValue)
            {
                if (delayAt.Value <= DateTimeOffset.Now)
                {
                    delayAt = null;
                }
                else
                {
                    additionalItems.Add(EventBusConsts.DelayAtHeaderKey, delayAt.ToString());
                }
            }

            MessageStorageModel messageStorageModel = new()
            {
                MsgId            = msgId,
                Environment      = Options.Value.Environment,
                CreateTime       = now,
                ExpireTime       = null,
                EventHandlerName = null,
                EventName        = EventNameRuler.GetName(typeof(TEvent)),
                RetryCount       = 0,
                Status           = MessageStatus.Scheduled,
                IsLocking        = false,
                LockEnd          = null,
                EventItems       = MessageSerializer.Serialize(additionalItems),
                EventBody        = MessageSerializer.Serialize(@event),
                DelayAt          = delayAt,
            };

            MessageTransferModel messageTransferModel = new()
            {
                EventName   = messageStorageModel.EventName,
                Environment = Options.Value.Environment,
                MsgId       = messageStorageModel.MsgId,
                MsgBody     = messageStorageModel.EventBody,
                Items       = additionalItems,
                SendAt      = now,
                DelayAt     = delayAt,
            };

            // 消息持久化
            await MessageStorage.SavePublishedAsync(messageStorageModel, transactionContext, cancellationToken).ConfigureAwait(false);
            // 先持久化,持久化没有错误,异步发送消息
            // 异步发送消息,启动时如果失败,最多循环5次
            _ = Task.Run(async () => await Start(transactionContext, messageStorageModel, messageTransferModel, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }

        private async Task Start(ITransactionContext transactionContext, MessageStorageModel messageStorageModel, MessageTransferModel messageTransferModel, CancellationToken cancellationToken)
        {
            // 等待事务完成
            var now = DateTimeOffset.Now;
            while (!cancellationToken.IsCancellationRequested && transactionContext != null && !transactionContext.IsDone())
            {
                if ((DateTimeOffset.Now - now).TotalSeconds > Options.Value.TransactionCommitTimeout)
                {
                    Logger.LogDebug($"[EventBus] message \"{messageStorageModel}\" transaction commit timeout");
                    return;
                }

                await Task.Delay(10).ConfigureAwait(false);
            }

            // 事务提交了,判断消息数据是否已提交
            try
            {
                // 消息未提交, 不执行任何操作
                if (!await MessageStorage.IsCommittedAsync(messageStorageModel.MsgId, cancellationToken).ConfigureAwait(false))
                {
                    Logger.LogDebug($"[EventBus] message \"{messageStorageModel.Id}\" has been rollback");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, $"[EventBus] query message \"{messageStorageModel.Id}\" commit state occur error");
                // 查询异常，将由重试器处理
                return;
            }

            // 执行失败的次数
            var failCount = 1;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (messageStorageModel.CreateTime <= DateTimeOffset.Now.AddSeconds(-Options.Value.TransactionCommitTimeout))
                {
                    // 超过时间了,就不管了,状态还是SCHEDULED
                    return;
                }

                // 执行真正的消息发送
                var handleResult = await PublishHandler.HandleAsync(messageTransferModel, messageStorageModel, cancellationToken).ConfigureAwait(false);
                if (!handleResult.Success)
                {
                    failCount++;
                }
                else
                {
                    return;
                }

                if (failCount > 5)
                {
                    // 将由重试器处理,为了减少线程消耗这里不再精准执行
                    return;
                }
            }
        }
    }
}