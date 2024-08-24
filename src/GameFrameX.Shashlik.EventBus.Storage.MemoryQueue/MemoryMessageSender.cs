using System.Threading.Tasks;
using Shashlik.EventBus;

namespace GameFrameX.Shashlik.EventBus.Storage.MemoryQueue;

/// <summary>
///     消息发送处理类
/// </summary>
public class MemoryMessageSender : IMessageSender
{
    public Task SendAsync(MessageTransferModel message)
    {
        InternalMemoryQueue.Send(message);
        return Task.CompletedTask;
    }
}