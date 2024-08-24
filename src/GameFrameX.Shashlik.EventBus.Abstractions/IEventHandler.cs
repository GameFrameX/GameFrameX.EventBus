using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameFrameX.Shashlik.EventBus.Abstractions
{
    /// <summary>
    /// 事件处理器
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IEvent
    {
        /// <summary>
        /// 执行事件处理
        /// </summary>
        /// <param name="event">事件实例</param>
        /// <param name="additionalItems">附加数据</param>
        /// <returns></returns>
        Task Execute(TEvent @event, IDictionary<string, string> additionalItems);
    }
}