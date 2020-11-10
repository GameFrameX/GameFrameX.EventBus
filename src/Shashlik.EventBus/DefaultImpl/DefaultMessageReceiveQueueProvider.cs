﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shashlik.EventBus.DefaultImpl
{
    public class DefaultMessageReceiveQueueProvider : IMessageReceiveQueueProvider
    {
        public DefaultMessageReceiveQueueProvider(
            IEventHandlerInvoker eventHandlerInvoker,
            ILogger<DefaultMessageReceiveQueueProvider> logger,
            IOptionsMonitor<EventBusOptions> options,
            IMessageStorage messageStorage)
        {
            EventHandlerInvoker = eventHandlerInvoker;
            Logger = logger;
            Options = options;
            MessageStorage = messageStorage;
        }

        private IEventHandlerInvoker EventHandlerInvoker { get; }
        private ILogger<DefaultMessageReceiveQueueProvider> Logger { get; }
        private IOptionsMonitor<EventBusOptions> Options { get; }
        private IMessageStorage MessageStorage { get; }

        public void Enqueue(MessageStorageModel messageStorageModel, IDictionary<string, string> items,
            EventHandlerDescriptor descriptor, CancellationToken cancellationToken)
        {
            // 延迟1秒再执行
            Thread.Sleep(1000);

            Task.Run(async () =>
            {
                // 执行失败的次数
                var failCount = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (messageStorageModel.CreateTime <=
                        DateTime.Now.AddSeconds(-Options.CurrentValue.ConfirmTransactionSeconds))
                        // 超过时间了,就不管了,状态还是SCHEDULED
                        return;
                    if (failCount > 4)
                        // 最多失败5次就不再重试了,如果消息已经写入那么5分钟后由重试器执行,如果没写入那就撒事也没有
                    {
                        // 消息处理没问题就更新数据库状态
                        try
                        {
                            await MessageStorage.UpdateReceived(messageStorageModel.MsgId, MessageStatus.Failed,
                                failCount, null, cancellationToken);
                        }
                        catch
                        {
                            // ignored
                        }

                        return;
                    }

                    try
                    {
                        // 执行事件消费
                        EventHandlerInvoker.Invoke(messageStorageModel, items, descriptor);
                        // 消息处理没问题就更新数据库状态
                        await MessageStorage.UpdateReceived(messageStorageModel.MsgId, MessageStatus.Succeeded, 0,
                            DateTime.Now.AddHours(Options.CurrentValue.SucceedExpireHour), cancellationToken);

                        return;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        Logger.LogError(ex,
                            $"[EventBus] message receive error, will try again later, event: {descriptor.EventName}, handler: {descriptor.EventHandlerName}, msgId: {messageStorageModel.MsgId}");
                    }
                }
            }, cancellationToken);
        }
    }
}