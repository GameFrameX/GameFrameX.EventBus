using System.Threading.Tasks;

namespace Shashlik.EventBus.Storage.MemoryQueue;

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