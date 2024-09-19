﻿using System;
using System.Collections.Concurrent;
using GameFrameX.EventBus.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GameFrameX.EventBus.Storage.RabbitMQ;

public class DefaultRabbitMQConnection : IRabbitMQConnection, IDisposable
{
    private const    string                            FailRetryHeaderKey = "FailCounter";
    private readonly ConcurrentDictionary<int, IModel> _channels;

    private readonly Lazy<IConnection>                                   _connection;
    private readonly ConcurrentDictionary<string, EventingBasicConsumer> _consumers;
    private readonly ILogger<RabbitMQMessageSender>                      _logger;

    public DefaultRabbitMQConnection(IOptionsMonitor<EventBusRabbitMQOptions> options,
        ILogger<RabbitMQMessageSender>                                        logger)
    {
        Options     = options;
        _logger     = logger;
        _connection = new Lazy<IConnection>(Get, true);
        _channels   = new ConcurrentDictionary<int, IModel>();
        _consumers  = new ConcurrentDictionary<string, EventingBasicConsumer>();
    }

    private IOptionsMonitor<EventBusRabbitMQOptions> Options { get; }

    private IConnection Connection
    {
        get { return _connection.Value; }
    }

    public void Dispose()
    {
        try
        {
            foreach (var item in _channels.Values)
            {
                item.Dispose();
            }

            Connection.Dispose();
            _channels.Clear();
        }
        catch
        {
            // ignore
        }
    }

    public IModel GetChannel()
    {
        var id = Environment.CurrentManagedThreadId;
        var channel = _channels.GetOrAdd(id, r =>
                                             {
                                                 var c = Connection.CreateModel();
                                                 InitChannel(c);
                                                 return c;
                                             });
        if (!channel.IsClosed)
        {
            return channel;
        }

        try
        {
            channel.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        channel = _channels[id] = Connection.CreateModel();
        InitChannel(channel);

        return channel;
    }

    public EventingBasicConsumer CreateConsumer(string eventHandlerName)
    {
        return _consumers.GetOrAdd(eventHandlerName, r => new EventingBasicConsumer(GetChannel()));
    }

    private void InitChannel(IModel channel)
    {
        channel.ConfirmSelect();
        channel.BasicReturn += (_, args) =>
                               {
                                   var counter    = args.BasicProperties.Headers.GetOrDefault(FailRetryHeaderKey);
                                   var counterInt = counter?.ParseTo<int>() ?? 0;
                                   if (counterInt >= 60)
                                   {
                                       _logger.LogError(
                                           $"[EventBus-RabbitMQ] send msg was returned and will not be try again: {args.RoutingKey}, ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}");
                                       return;
                                   }

                                   args.BasicProperties.Headers[FailRetryHeaderKey] = counterInt + 1;
                                   _logger.LogWarning(
                                       $"[EventBus-RabbitMQ] send msg was returned and will try again: {args.RoutingKey}, ReplyCode: {args.ReplyCode}, ReplyText: {args.ReplyText}");

                                   // 被退回的消息重试发送
                                   channel.BasicPublish(Options.CurrentValue.Exchange, args.RoutingKey, true, args.BasicProperties,
                                                        args.Body);
                                   // 等待消息发布确认or die,确保消息发送环节不丢失
                                   channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(Options.CurrentValue.ConfirmTimeout));
                               };
        // 交换机定义,类型topic
        channel.ExchangeDeclare(Options.CurrentValue.Exchange, ExchangeType.Direct, true);
    }

    private IConnection Get()
    {
        ConnectionFactory factory;
        if (Options.CurrentValue.ConnectionFactory != null)
        {
            factory = Options.CurrentValue.ConnectionFactory();
        }
        else
        {
            factory = new ConnectionFactory
            {
                Password    = Options.CurrentValue.Password,
                HostName    = Options.CurrentValue.Host,
                UserName    = Options.CurrentValue.UserName,
                Port        = Options.CurrentValue.Port,
                VirtualHost = Options.CurrentValue.VirtualHost,
            };
        }

        return factory.CreateConnection();
    }
}