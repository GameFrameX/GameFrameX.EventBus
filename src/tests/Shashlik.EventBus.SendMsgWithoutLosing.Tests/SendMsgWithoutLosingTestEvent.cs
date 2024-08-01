﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shashlik.EventBus.Abstractions;

namespace Shashlik.EventBus.SendMsgWithoutLosing.Tests
{
    public class SendMsgWithoutLosingTestEvent : IEvent
    {
        public string Name { get; set; }
    }

    public class SendMsgWithoutLosingTestEventHandler : IEventHandler<SendMsgWithoutLosingTestEvent>
    {
        public Task Execute(SendMsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            throw  new Exception();
        }
    }
}