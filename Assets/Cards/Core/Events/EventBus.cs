using System;
using System.Collections.Generic;
using Cards.Zones;

namespace Cards.Core.Events
{
    /// <summary>
    /// Subscription token returned by EventBus.Subscribe.
    /// Store it and pass to Unsubscribe to remove a specific handler.
    /// </summary>
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

    public static class EventBus
    {
        private class Subscription
        {
            public int Id;
            public Action<object> Handler;
        }

        private static readonly Dictionary<Type, List<Subscription>> _subscribers = new Dictionary<Type, List<Subscription>>();
        private static int _nextId = 0;

        /// <summary>
        /// Subscribe to an event. Returns an EventToken that can be passed to Unsubscribe.
        /// The token can be safely ignored if you only rely on Clear() for cleanup.
        /// </summary>
        public static EventToken Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_subscribers.TryGetValue(type, out var list))
            {
                list = new List<Subscription>();
                _subscribers[type] = list;
            }

            int id = _nextId++;
            list.Add(new Subscription
            {
                Id = id,
                Handler = (obj) => handler((T)obj)
            });

            return new EventToken(type, id);
        }

        /// <summary>
        /// Remove a specific subscription by its token.
        /// </summary>
        public static void Unsubscribe(EventToken token)
        {
            if (token == null) return;
            if (_subscribers.TryGetValue(token.EventType, out var list))
            {
                list.RemoveAll(s => s.Id == token.Id);
            }
        }

        public static void Publish<T>(T gameEvent)
        {
            if (_subscribers.TryGetValue(typeof(T), out var list))
            {
                // Snapshot to safely handle subscribe/unsubscribe during publish
                var snapshot = list.ToArray();
                foreach (var sub in snapshot)
                {
                    sub.Handler?.Invoke(gameEvent);
                }
            }
        }
        
        public static void Clear()
        {
            _subscribers.Clear();
            _nextId = 0;
        }
    }

    // --- Core Events ---
    
    public class RequestMoveCardEvent 
    { 
        public CardEntity Card; 
        public CardZone SourceZone;
        public ZoneId? SourceZoneId;
        public ZoneId TargetZoneId; 
        public bool UseAnimation = true;
    }
    
    public class CardPlayedEvent 
    { 
        public CardEntity Card; 
    }
    
    public class CardDiedEvent 
    { 
        public CardEntity Card; 
    }

    public class CardClickedEvent
    {
        public CardEntity Card;
    }
}
