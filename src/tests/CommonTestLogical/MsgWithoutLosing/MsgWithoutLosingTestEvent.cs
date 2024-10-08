﻿using System.Collections.Generic;
using System.Threading.Tasks;
using GameFrameX.EventBus.Abstractions;
using Shashlik.EventBus;

namespace CommonTestLogical.MsgWithoutLosing
{
    public class MsgWithoutLosingTestEvent : IEvent
    {
        public string Name { get; set; }
    }

    public class MsgWithoutLosingEventHandler : IEventHandler<MsgWithoutLosingTestEvent>
    {
        public Task Execute(MsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler2 : IEventHandler<MsgWithoutLosingTestEvent>
    {
        public Task Execute(MsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler3 : IEventHandler<MsgWithoutLosingTestEvent>
    {
        public Task Execute(MsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }

    public class ExceptionLogicalTestEventHandler4 : IEventHandler<MsgWithoutLosingTestEvent>
    {
        public Task Execute(MsgWithoutLosingTestEvent @event, IDictionary<string, string> items)
        {
            return Task.CompletedTask;
        }
    }
}