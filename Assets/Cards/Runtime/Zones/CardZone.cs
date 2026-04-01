using System;
using System.Collections.Generic;
using Cards.Core;
using Cards.Rules.Interactions;
using Cards.Services;

namespace Cards.Zones
{
    public class CardZone
    {
        public ZoneId ZoneId { get; }
        public string ZoneName { get; }

        private readonly List<CardInstance> cards = new List<CardInstance>();
        public IReadOnlyList<CardInstance> Cards => cards;

        public List<IInteractionRule> Rules { get; } = new List<IInteractionRule>();

        public event Action<CardInstance, int> OnCardAdded;
        public event Action<CardInstance> OnCardRemoved;
        public event Action OnShuffled;

        public int Count => cards.Count;
        public bool IsEmpty => cards.Count == 0;

        public CardZone(ZoneId zoneId, string zoneName = null)
        {
            ZoneId = zoneId;
            ZoneName = zoneName;
        }

        public void AddRule(IInteractionRule rule)
        {
            if (rule != null && !Rules.Contains(rule))
            {
                Rules.Add(rule);
            }
        }

        public void AddCard(CardInstance card)
        {
            if (card == null || cards.Contains(card))
            {
                return;
            }

            cards.Add(card);
            card.CurrentZone = this;
            card.CurrentZoneId = ZoneId;
            OnCardAdded?.Invoke(card, cards.Count - 1);
        }

        public void AddCards(IEnumerable<CardInstance> newCards)
        {
            if (newCards == null)
            {
                return;
            }

            foreach (CardInstance card in newCards)
            {
                AddCard(card);
            }
        }

        public void RemoveCard(CardInstance card)
        {
            if (card == null || !cards.Remove(card))
            {
                return;
            }

            card.CurrentZone = null;
            card.CurrentZoneId = null;
            OnCardRemoved?.Invoke(card);
        }

        public CardInstance DrawTopCard()
        {
            if (IsEmpty)
            {
                return null;
            }

            int topIndex = cards.Count - 1;
            CardInstance drawnCard = cards[topIndex];
            cards.RemoveAt(topIndex);
            drawnCard.CurrentZone = null;
            drawnCard.CurrentZoneId = null;
            OnCardRemoved?.Invoke(drawnCard);
            return drawnCard;
        }

        public List<CardInstance> TakeAllCards()
        {
            var takenCards = new List<CardInstance>(cards);
            foreach (CardInstance card in takenCards)
            {
                card.CurrentZone = null;
                card.CurrentZoneId = null;
                OnCardRemoved?.Invoke(card);
            }

            cards.Clear();
            return takenCards;
        }

        public void Shuffle(IRandom random)
        {
            if (random == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                int randomIndex = random.Range(i, cards.Count);
                CardInstance temp = cards[i];
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }

            OnShuffled?.Invoke();
        }

        public bool Contains(CardInstance card)
        {
            return cards.Contains(card);
        }
    }
}
