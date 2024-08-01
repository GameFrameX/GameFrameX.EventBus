using Shashlik.EventBus;
using Shashlik.EventBus.Abstractions;

namespace CommonTestLogical.TestEvents
{
    public class XaEvent : IEvent
    {
        public string Name { get; set; }
    }
}