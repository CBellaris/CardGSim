using UnityEngine;
using Cards.Core;
using Cards.Core.Events;
using Cards.Zones;

namespace Cards.Rules.Handlers
{
    /// <summary>
    /// 处理场上卡牌死亡的逻辑
    /// 解耦自 GameManager
    /// </summary>
    public static class CardDeathHandler
    {
        public static void Initialize()
        {
            EventBus.Subscribe<CardDiedEvent>(OnCardDied);
        }

        private static void OnCardDied(CardDiedEvent evt)
        {
            CardZone boardZone = evt.Card.CurrentZone;
            CardZone exhaustPile = ZoneRegistry.Get(ZoneId.PlayerExhaustPile);

            if (boardZone != null && evt.Card.CurrentZoneId.HasValue && evt.Card.CurrentZoneId.Value.IsBoard() && boardZone.Contains(evt.Card))
            {
                if (evt.Card.Owner == CardOwner.Player && exhaustPile != null)
                {
                    ZoneTransferService.MoveCard(evt.Card, exhaustPile, true, boardZone);
                }
                else
                {
                    boardZone.RemoveCard(evt.Card, true);
                }
            }
        }
    }
}