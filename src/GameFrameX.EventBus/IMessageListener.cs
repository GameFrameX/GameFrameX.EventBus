﻿using System.Threading;
using System.Threading.Tasks;
using Shashlik.EventBus;

namespace GameFrameX.EventBus
{
    /// <summary>
    /// 消息监听执行器
    /// </summary>
    public interface IMessageListener
    {
        /// <summary>
        /// 消息接收处理
        /// </summary>
        /// <param name="eventHandlerName">事件处理类名称</param>
        /// <param name="messageTransferModel">消息传输模型</param>
        /// <param name="cancellationToken"></param>
        /// <returns>消息接收处理结果</returns>
        Task<MessageReceiveResult> OnReceiveAsync(string eventHandlerName, MessageTransferModel messageTransferModel, CancellationToken cancellationToken);
    }
}