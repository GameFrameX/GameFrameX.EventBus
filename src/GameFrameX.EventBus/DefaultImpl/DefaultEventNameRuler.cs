using System;
using System.Reflection;
using GameFrameX.EventBus.Abstractions;
using Microsoft.Extensions.Options;

namespace GameFrameX.EventBus.DefaultImpl
{
    public class DefaultNameRuler : IEventNameRuler, IEventHandlerNameRuler
    {
        public DefaultNameRuler(IOptions<EventBusOptions> options)
        {
            Options = options;
        }

        private IOptions<EventBusOptions> Options { get; }

        public string GetName(Type eventType)
        {
            var name = eventType.GetCustomAttribute<EventBusNameAttribute>()?.Name;
            name ??= eventType.Name;

            return $"{name}.{Options.Value.Environment}";
        }
    }
}