using System.Collections.Generic;
using UnityEngine;
using Cards.Zones.Layouts;
using Cards.Rules.Interactions;
using Cards.Core;

namespace Cards.Zones
{
    /// <summary>
    /// 通用卡牌区域
    /// 可以是牌库、手牌、弃牌堆、战场等
    /// </summary>
    public class CardZone
    {
        public ZoneId ZoneId { get; }
        public string ZoneName { get; }
        private List<CardEntity> cards = new List<CardEntity>();
        public IReadOnlyList<CardEntity> Cards => cards;

        public IZoneLayout Layout { get; set; }
        
        // 挂载在这个区域上的交互规则
        public List<IInteractionRule> Rules { get; private set; } = new List<IInteractionRule>();

        public int Count => cards.Count;
        public bool IsEmpty => cards.Count == 0;

        public CardZone(ZoneId zoneId, IZoneLayout layout = null)
            : this(zoneId, null, layout)
        {
        }

        public CardZone(ZoneId zoneId, string zoneName, IZoneLayout layout = null)
        {
            ZoneId = zoneId;
            ZoneName = zoneName;
            this.Layout = layout;
        }

        public void AddRule(IInteractionRule rule)
        {
            if (!Rules.Contains(rule))
            {
                Rules.Add(rule);
            }
        }

        public void AddCard(CardEntity card, bool useAnimation = false)
        {
            if (card != null && !cards.Contains(card))
            {
                cards.Add(card);
                card.SetCurrentZone(this);
                
                if (Layout != null)
                {
                    if (Layout.SupportsIncrementalAdd)
                    {
                        Layout.OnCardAdded(card, cards.Count - 1, useAnimation);
                    }
                    else
                    {
                        Layout.Arrange(cards, useAnimation);
                    }
                }
            }
        }

        public void AddCards(IEnumerable<CardEntity> newCards, bool useAnimation = false)
        {
            if (newCards == null) return;

            foreach (CardEntity card in newCards)
            {
                if (card == null || cards.Contains(card)) continue;
                cards.Add(card);
                card.SetCurrentZone(this);
            }
            UpdateLayout(useAnimation);
        }

        public void RemoveCard(CardEntity card, bool useAnimation = false)
        {
            if (cards.Contains(card))
            {
                cards.Remove(card);
                card.SetCurrentZone(null);
                UpdateLayout(useAnimation);
            }
        }

        /// <summary>
        /// 抽牌：从区域顶部（列表末尾）取出一张牌
        /// </summary>
        public CardEntity DrawTopCard()
        {
            if (IsEmpty) return null;

            int topIndex = cards.Count - 1;
            CardEntity drawnCard = cards[topIndex];
            cards.RemoveAt(topIndex);
            drawnCard.SetCurrentZone(null);
            
            if (Layout != null && !Layout.SupportsIncrementalAdd)
            {
                UpdateLayout(true);
            }

            return drawnCard;
        }

        public List<CardEntity> TakeAllCards()
        {
            List<CardEntity> takenCards = new List<CardEntity>(cards);
            foreach (CardEntity card in takenCards)
            {
                card.SetCurrentZone(null);
            }
            cards.Clear();
            UpdateLayout(false);
            return takenCards;
        }

        public void Shuffle()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                CardEntity temp = cards[i];
                int randomIndex = Random.Range(i, cards.Count);
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }
            UpdateLayout();
        }

        public void UpdateLayout(bool useAnimation = false)
        {
            if (Layout != null)
            {
                Layout.Arrange(cards, useAnimation);
            }
        }

        public bool Contains(CardEntity card)
        {
            return cards.Contains(card);
        }
    }
}
