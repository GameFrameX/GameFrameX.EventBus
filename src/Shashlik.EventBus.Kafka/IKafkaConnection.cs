﻿using System;
using Confluent.Kafka;

namespace Shashlik.EventBus.Kafka
{
    public interface IKafkaConnection : IDisposable
    {
        /// <summary>
        /// 获取消息生产者
        /// </summary>
        /// <returns></returns>
        IProducer<string, byte[]> GetProducer();

        /// <summary>
        /// 创建消息消费者
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        IConsumer<string, byte[]> CreateCunsumer(string groupId);
    }
}