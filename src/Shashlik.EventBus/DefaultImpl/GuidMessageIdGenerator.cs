using System;

namespace Shashlik.EventBus.DefaultImpl
{
    public class GuidMessageIdGenerator : IMessageIdGenerator
    {
        public string GenerateId()
        {
            return Guid.NewGuid().ToString("n");
        }
    }
}