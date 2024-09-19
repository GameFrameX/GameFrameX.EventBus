using Pulsar.Client.Api;

namespace GameFrameX.EventBus.Storage.Pulsar;

public interface IPulsarConnection
{
    IProducer<byte[]> GetProducer(string topic);

    IConsumer<byte[]> GetConsumer(string topic, string eventHandlerName);
}