using Shashlik.EventBus;
using Shashlik.EventBus.Abstractions;

namespace SampleBase
{
    public class Event1 : IEvent
    {
        public string Name { get; set; }
    }

    public class DelayEvent : IEvent
    {
        public string Name { get; set; }
    }
}