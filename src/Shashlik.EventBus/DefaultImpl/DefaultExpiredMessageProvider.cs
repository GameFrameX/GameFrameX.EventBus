﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Shashlik.Utils.Helpers;

namespace Shashlik.EventBus.DefaultImpl
{
    /// <summary>
    /// 已过期的消息处理
    /// </summary>
    public class DefaultExpiredMessageProvider : IExpiredMessageProvider
    {
        public DefaultExpiredMessageProvider(IMessageStorage messageStorage)
        {
            MessageStorage = messageStorage;
        }

        private IMessageStorage MessageStorage { get; }

        public Task DoDelete(CancellationToken cancellationToken)
        {
            // 每个小时执行1次删除
            TimerHelper.SetInterval(
                async () =>
                {
                    await MessageStorage.DeleteExpires(cancellationToken).ConfigureAwait(false);
                    GC.Collect();
                },
                TimeSpan.FromHours(1),
                cancellationToken);

            return Task.CompletedTask;
        }
    }
}