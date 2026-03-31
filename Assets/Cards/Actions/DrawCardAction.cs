using System.Collections;
using UnityEngine;
using Cards.Zones;
using Cards.Core;

namespace Cards.Actions
{
    public class DrawCardAction : GameAction
    {
        public override IEnumerator ExecuteRoutine()
        {
            CardZone drawPile = ZoneRegistry.Get(ZoneId.PlayerDrawPile);
            CardZone discardPile = ZoneRegistry.Get(ZoneId.PlayerDiscardPile);
            CardZone handZone = ZoneRegistry.Get(ZoneId.PlayerHand);

            if (drawPile == null || discardPile == null || handZone == null)
            {
                Debug.LogError("[DrawCardAction] 找不到必要的区域！");
                IsCompleted = true;
                yield break;
            }

            if (drawPile.IsEmpty)
            {
                if (discardPile.IsEmpty)
                {
                    Debug.Log("抽牌堆和弃牌堆都为空，无法抽牌！");
                    IsCompleted = true;
                    yield break;
                }

                ActionManager.Instance.AddAction(new ReshuffleAction(drawPile, discardPile));
                ActionManager.Instance.AddAction(new DrawCardAction());
                IsCompleted = true;
                yield break;
            }

            CardEntity drawnCard = drawPile.DrawTopCard();

            var request = new Cards.Rules.Interactions.InteractionRequest
            {
                Type = Cards.Rules.Interactions.InteractionType.ZoneTransfer,
                SourceCard = drawnCard,
                SourceZone = drawPile,
                SourceZoneId = ZoneId.PlayerDrawPile,
                TargetZone = handZone,
                TargetZoneId = ZoneId.PlayerHand
            };

            Cards.Rules.Interactions.RuleEngine.ProcessInteraction(request);

            Debug.Log($"[Action] 抽了1张牌: {drawnCard.CurrentCardData.CardName}，抽牌堆剩余: {drawPile.Count}");

            yield return new WaitForSeconds(0.35f);

            IsCompleted = true;
        }
    }
}
