using System.Text;
using Shashlik.EventBus;
using Shashlik.EventBus.Abstractions;

namespace Shashlik.Dashboard.Demo;

public class TestEventHandler : IEventHandler<TestEvent>
{
    public Task Execute(TestEvent @event, IDictionary<string, string> additionalItems)
    {
        Console.WriteLine($"{DateTime.Now},Executing, Title: {@event.Title}, Data: {Encoding.UTF8.GetString(@event.Data)}");

        var dateTimeOffset = additionalItems.GetSendTime().AddSeconds(30);

        if (DateTimeOffset.Now < dateTimeOffset)
        {
            // throw new Exception();
        }

        return Task.CompletedTask;
    }
}