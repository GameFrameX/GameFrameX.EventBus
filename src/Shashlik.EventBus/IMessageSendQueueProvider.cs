﻿using System.Threading;

namespace Shashlik.EventBus
{
    /// <summary>
    /// 消息发送处理队列
    /// </summary>
    public interface IMessageSendQueueProvider
    {
        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="messageTransferModel"></param>
        /// <param name="messageStorageModel">存储消息模型</param>
        /// <param name="cancellationToken"></param>
        void Enqueue(MessageTransferModel messageTransferModel, MessageStorageModel messageStorageModel,
            CancellationToken cancellationToken);
    }
}