﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shashlik.EventBus;
using Shashlik.EventBus.Utils;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

// ReSharper disable AsyncVoidLambda

namespace GameFrameX.Shashlik.EventBus.Storage.RabbitMQ;

public class RabbitMQEventSubscriber : IEventSubscriber
{
    public RabbitMQEventSubscriber(IOptionsMonitor<EventBusRabbitMQOptions> options,
        IRabbitMQConnection                                                 connection,        ILogger<RabbitMQEventSubscriber> logger,
        IMessageSerializer                                                  messageSerializer, IMessageListener                 messageListener)
    {
        Options           = options;
        Connection        = connection;
        Logger            = logger;
        MessageSerializer = messageSerializer;
        MessageListener   = messageListener;
    }

    private IOptionsMonitor<EventBusRabbitMQOptions> Options           { get; }
    private ILogger<RabbitMQEventSubscriber>         Logger            { get; }
    private IMessageSerializer                       MessageSerializer { get; }
    private IMessageListener                         MessageListener   { get; }
    private IRabbitMQConnection                      Connection        { get; }

    public Task SubscribeAsync(EventHandlerDescriptor eventHandlerDescriptor, CancellationToken cancellationToken)
    {
        var channel = Connection.GetChannel();
        // 注册基础通信交换机,类型topic
        channel.ExchangeDeclare(Options.CurrentValue.Exchange, "topic", true);
        // 定义队列
        channel.QueueDeclare(eventHandlerDescriptor.EventHandlerName, true, false, false);
        // 绑定队列到交换机以及routing key
        channel.QueueBind(eventHandlerDescriptor.EventHandlerName, Options.CurrentValue.Exchange,
                          eventHandlerDescriptor.EventName);

        var eventName        = eventHandlerDescriptor.EventName;
        var eventHandlerName = eventHandlerDescriptor.EventHandlerName;

        var consumer = Connection.CreateConsumer(eventHandlerName);
        consumer.Received += async (_, e) =>
                             {
                                 if (cancellationToken.IsCancellationRequested)
                                 {
                                     return;
                                 }

                                 MessageTransferModel message;
                                 try
                                 {
                                     message = MessageSerializer.Deserialize<MessageTransferModel>(e.Body.ToArray());
                                 }
                                 catch (Exception exception)
                                 {
                                     Logger.LogError(exception, "[EventBus-RabbitMQ] deserialize message from rabbit error");
                                     return;
                                 }

                                 if (message is null)
                                 {
                                     Logger.LogError("[EventBus-RabbitMQ] deserialize message from rabbit error");
                                     return;
                                 }

                                 if (message.EventName != eventName)
                                 {
                                     Logger.LogError(
                                         $"[EventBus-RabbitMQ] received invalid event name \"{message.EventName}\", expect \"{eventName}\"");
                                     return;
                                 }

                                 Logger.LogDebug(
                                     $"[EventBus-RabbitMQ] received msg: {message}");

                                 // 处理消息
                                 var res = await MessageListener
                                                 .OnReceiveAsync(eventHandlerName, message, cancellationToken)
                                                 .ConfigureAwait(false);
                                 if (res == MessageReceiveResult.Success)
                                     // 一定要在消息接收处理完成后才确认ack
                                 {
                                     channel.BasicAck(e.DeliveryTag, false);
                                 }
                                 else
                                 {
                                     channel.BasicReject(e.DeliveryTag, true);
                                 }
                             };

        consumer.Registered += (_, _) =>
                               {
                                   Logger.LogInformation(
                                       $"[EventBus-RabbitMQ] event handler \"{eventHandlerName}\" has been registered");
                               };
        consumer.Shutdown += (_, _) =>
                             {
                                 Logger.LogWarning(
                                     $"[EventBus-RabbitMQ] event handler \"{eventHandlerName}\" has been shutdown");
                             };
        channel.BasicConsume(eventHandlerName, false, consumer);

        return Task.CompletedTask;
    }
}