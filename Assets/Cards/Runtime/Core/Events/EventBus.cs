using System;
using System.Collections.Generic;
using Cards.Services;
using Cards.Zones;

namespace Cards.Core.Events
{
    public class EventToken
    {
        internal Type EventType { get; }
        internal int Id { get; }

        internal EventToken(Type eventType, int id)
        {
            EventType = eventType;
            Id = id;
        }
    }

    public class EventBus : IEventBus
    {
        private class Subscription
        {
            public int Id;
            public Action<object> Handler;
        }

        private readonly Dictionary<Type, List<Subscription>> subscribers = new Dictionary<Type, List<Subscription>>();
        private int nextId;

        public EventToken Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!subscribers.TryGetValue(type, out List<Subscription> list))
            {
                list = new List<Subscription>();
                subscribers[type] = list;
            }

            int id = nextId++;
            list.Add(new Subscription
            {
                Id = id,
                Handler = obj => handler((T)obj)
            });

            return new EventToken(type, id);
        }

        public void Unsubscribe(EventToken token)
        {
            if (token == null)
            {
                return;
            }

            if (subscribers.TryGetValue(token.EventType, out List<Subscription> list))
            {
                list.RemoveAll(subscription => subscription.Id == token.Id);
            }
        }

        public void Publish<T>(T gameEvent)
        {
            if (!subscribers.TryGetValue(typeof(T), out List<Subscription> list))
            {
                return;
            }

            Subscription[] snapshot = list.ToArray();
            foreach (Subscription subscription in snapshot)
            {
                subscription.Handler?.Invoke(gameEvent);
            }
        }

        public void Clear()
        {
            subscribers.Clear();
            nextId = 0;
        }
    }

    public class RequestMoveCardEvent
    {
        public Cards.Core.CardInstance Card;
        public CardZone SourceZone;
        public ZoneId? SourceZoneId;
        public ZoneId TargetZoneId;
    }

    public class CardPlayedEvent
    {
        public Cards.Core.CardInstance Card;
    }

    public class CardDiedEvent
    {
        public Cards.Core.CardInstance Card;
    }

    public class CardClickedEvent
    {
        public Cards.Core.CardInstance Card;
    }
}
