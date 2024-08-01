﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shashlik.EventBus.Storage.RabbitMQ;

public interface IRabbitMQConnection
{
    IModel GetChannel();

    EventingBasicConsumer CreateConsumer(string eventHandlerName);
}