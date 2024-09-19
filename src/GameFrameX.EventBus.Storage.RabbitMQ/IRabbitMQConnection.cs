using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GameFrameX.EventBus.Storage.RabbitMQ;

public interface IRabbitMQConnection
{
    IModel GetChannel();

    EventingBasicConsumer CreateConsumer(string eventHandlerName);
}