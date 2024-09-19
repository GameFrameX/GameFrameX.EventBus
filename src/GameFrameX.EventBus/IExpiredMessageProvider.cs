using System.Threading;
using System.Threading.Tasks;

namespace GameFrameX.EventBus
{
    /// <summary>
    /// 已过期的消息处理
    /// </summary>
    public interface IExpiredMessageProvider
    {
        Task DoDeleteAsync(CancellationToken cancellationToken);
    }
}