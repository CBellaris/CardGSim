using System;
using Cards.Core.Events;

namespace Cards.Services
{
    public interface IEventBus
    {
        EventToken Subscribe<T>(Action<T> handler);
        void Unsubscribe(EventToken token);
        void Publish<T>(T gameEvent);
        void Clear();
    }
}
