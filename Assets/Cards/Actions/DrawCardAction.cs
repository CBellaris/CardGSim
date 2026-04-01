using System.Collections;
using UnityEngine;
using Cards.Zones;
using Cards.Core;
using Cards.Services;

namespace Cards.Actions
{
    public class DrawCardAction : GameAction
    {
        private float animationScale = 1f;

        public override void Execute(GameContext ctx)
        {
            animationScale = ResolveAnimationScale(ctx);
            CardZone drawPile = ctx?.Zones?.Get(ZoneId.PlayerDrawPile);
            CardZone discardPile = ctx?.Zones?.Get(ZoneId.PlayerDiscardPile);
            CardZone handZone = ctx?.Zones?.Get(ZoneId.PlayerHand);

            if (drawPile == null || discardPile == null || handZone == null)
            {
                LogError(ctx, "[DrawCardAction] 找不到必要的区域！");
                return;
            }

            if (drawPile.IsEmpty)
            {
                if (discardPile.IsEmpty)
                {
                    Log(ctx, "抽牌堆和弃牌堆都为空，无法抽牌！");
                    return;
                }

                Enqueue(ctx, new ReshuffleAction(drawPile, discardPile));
                Enqueue(ctx, new DrawCardAction());
                return;
            }

            CardInstance drawnCard = drawPile.DrawTopCard();

            var request = new Cards.Rules.Interactions.InteractionRequest
            {
                Context = ctx,
                Type = Cards.Rules.Interactions.InteractionType.ZoneTransfer,
                SourceCard = drawnCard,
                SourceZone = drawPile,
                SourceZoneId = ZoneId.PlayerDrawPile,
                TargetZone = handZone,
                TargetZoneId = ZoneId.PlayerHand
            };

            ctx?.Rules?.ProcessInteraction(request);

            Log(ctx, $"[Action] 抽了1张牌: {drawnCard.Data?.CardName}，抽牌堆剩余: {drawPile.Count}");
        }

        public override IEnumerator AnimateRoutine()
        {
            yield return new WaitForSeconds(0.35f * animationScale);
        }

        private static void Enqueue(GameContext ctx, GameAction action)
        {
            ctx?.Actions?.Enqueue(action);
        }

        private static void Log(GameContext ctx, string message)
        {
            if (ctx?.Logger != null)
            {
                ctx.Logger.Log(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        private static void LogError(GameContext ctx, string message)
        {
            if (ctx?.Logger != null)
            {
                ctx.Logger.LogError(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        private static float ResolveAnimationScale(GameContext ctx)
        {
            float scale = ctx?.AnimationPolicy?.TimeScale ?? 1f;
            return scale > 0f ? scale : 1f;
        }
    }
}
