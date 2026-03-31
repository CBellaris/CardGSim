using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cards.Zones;
using Cards.Core;

namespace Cards.Actions
{
    public class ReshuffleAction : GameAction
    {
        private CardZone drawPile;
        private CardZone discardPile;

        public ReshuffleAction(CardZone drawPile, CardZone discardPile)
        {
            this.drawPile = drawPile;
            this.discardPile = discardPile;
        }

        public override IEnumerator ExecuteRoutine()
        {
            Debug.Log("[Action] 抽牌堆为空，开始将弃牌堆洗入抽牌堆...");

            var recycledCards = discardPile.TakeAllCards();
            
            // 表现洗牌动画：一张一张飞回抽牌堆
            for (int i = recycledCards.Count - 1; i >= 0; i--)
            {
                CardEntity card = recycledCards[i];
                // 模型重置逻辑：因为从弃牌堆重新回到卡组，重置其血量状态
                if (card.Model != null)
                {
                    card.Model.ResetStats();
                }

                // 临时将牌放入抽牌堆，播放飞过去的动画
                drawPile.AddCard(card, true);
                yield return new WaitForSeconds(0.1f); // 洗牌飞行的间隔
            }

            // 飞行完毕后，稍微停顿一下
            yield return new WaitForSeconds(0.5f);

            // 实际的数据洗牌
            drawPile.Shuffle();
            
            // 重新排列表现
            //drawPile.UpdatePileVisuals(true);
            
            // 等待洗牌排列动画完成
            yield return new WaitForSeconds(0.3f); 

            Debug.Log("[Action] 洗牌完成！");
            IsCompleted = true;
        }
    }
}