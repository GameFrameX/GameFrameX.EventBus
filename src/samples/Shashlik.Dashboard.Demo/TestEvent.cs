using Shashlik.EventBus;
using Shashlik.EventBus.Abstractions;

namespace Shashlik.Dashboard.Demo;

public class TestEvent : IEvent
{
    public string? Title { get; set; }
    public byte[] Data { get; set; }
}