using System;

namespace Shashlik.EventBus.Abstractions
{
    /// <summary>
    /// 事件/事件处理名称定义
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EventBusNameAttribute : Attribute
    {
        /// <summary>
        /// 事件/事件处理名称
        /// </summary>
        /// <param name="name">事件/事件处理名称</param>
        public EventBusNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// 事件/事件处理名称
        /// </summary>
        public string Name { get; }
    }
}