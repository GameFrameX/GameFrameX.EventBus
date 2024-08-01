using Shashlik.EventBus;
using Shashlik.EventBus.Abstractions;

namespace SampleBase
{
    [EventBusName("Event2")]
    public class Event2222 : IEvent
    {
        public string Name { get; set; }
    }
}