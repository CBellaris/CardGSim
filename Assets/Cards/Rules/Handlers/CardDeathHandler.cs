using System;
using UnityEngine;
using Cards.Core;
using Cards.Core.Events;
using Cards.Services;
using Cards.Zones;

namespace Cards.Rules.Handlers
{
    /// <summary>
    /// 处理场上卡牌死亡的逻辑
    /// 解耦自 GameManager
    /// </summary>
    public class CardDeathHandler : IDisposable
    {
        private readonly GameContext context;
        private readonly EventToken subscription;

        public CardDeathHandler(GameContext context)
        {
            this.context = context;
            subscription = context?.Events?.Subscribe<CardDiedEvent>(OnCardDied);
        }

        public void Dispose()
        {
            context?.Events?.Unsubscribe(subscription);
        }

        private void OnCardDied(CardDiedEvent evt)
        {
            CardZone boardZone = evt.Card.CurrentZone;
            CardZone exhaustPile = context?.Zones?.Get(ZoneId.PlayerExhaustPile);

            if (boardZone != null && evt.Card.CurrentZoneId.HasValue && evt.Card.CurrentZoneId.Value.IsBoard() && boardZone.Contains(evt.Card))
            {
                if (evt.Card.Owner == CardOwner.Player && exhaustPile != null)
                {
                    context?.ZoneTransfers?.MoveCard(evt.Card, exhaustPile, boardZone);
                }
                else
                {
                    boardZone.RemoveCard(evt.Card);
                }
            }
        }
    }
}
