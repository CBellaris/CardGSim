using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cards.Zones;
using Cards.Core;
using Cards.Services;

namespace Cards.Actions
{
    public class ReshuffleAction : GameAction
    {
        private readonly CardZone drawPile;
        private readonly CardZone discardPile;
        private int recycledCount;
        private float animationScale = 1f;

        public ReshuffleAction(CardZone drawPile, CardZone discardPile)
        {
            this.drawPile = drawPile;
            this.discardPile = discardPile;
        }

        public override void Execute(GameContext ctx)
        {
            animationScale = ResolveAnimationScale(ctx);
            Log(ctx, "[Action] 抽牌堆为空，开始将弃牌堆洗入抽牌堆...");

            var recycledCards = discardPile.TakeAllCards();
            recycledCount = recycledCards.Count;

            for (int i = recycledCards.Count - 1; i >= 0; i--)
            {
                CardInstance card = recycledCards[i];
                if (card.Model != null)
                {
                    card.Model.ResetStats();
                }

                drawPile.AddCard(card);
            }

            drawPile.Shuffle(ctx?.Random);
            Log(ctx, "[Action] 洗牌完成！");
        }

        public override IEnumerator AnimateRoutine()
        {
            for (int i = 0; i < recycledCount; i++)
            {
                yield return new WaitForSeconds(0.1f * animationScale);
            }

            yield return new WaitForSeconds(0.5f * animationScale);
            yield return new WaitForSeconds(0.3f * animationScale);
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

        private static float ResolveAnimationScale(GameContext ctx)
        {
            float scale = ctx?.AnimationPolicy?.TimeScale ?? 1f;
            return scale > 0f ? scale : 1f;
        }
    }
}
