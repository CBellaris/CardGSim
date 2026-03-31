using UnityEngine;
using Cards.Data;
using Cards.Zones;

namespace Cards.Rules.Interactions
{
    /// <summary>
    /// 当带有“Entity”标签的卡牌进入 BoardZone 时，将其视为“打出实体”
    /// </summary>
    public class PlayEntityRule : IInteractionRule
    {
        public int Priority => 10;

        public bool Validate(InteractionRequest request)
        {
            // 只有当目标是 Board 时才处理
            if (request.TargetZoneId != ZoneId.PlayerBoard && request.TargetZoneId != ZoneId.EnemyBoard) return true;
            if (request.TargetZone == null) return false;
            
            // 确保卡牌有 Entity 标签
            if (request.SourceCard.CurrentCardData.Tags == null || !request.SourceCard.CurrentCardData.Tags.Contains(CardTag.Entity))
            {
                // 如果不是实体牌，但想拖到 Board 上，我们可以拦截这个行为（不允许放置）
                // 也可以返回 true 让其他规则处理（比如它是张攻击法术）
                return true; 
            }

            return true;
        }

        public void BeforeExecute(InteractionRequest request)
        {
            // 可以在这里处理费用扣除
        }

        public void Execute(InteractionRequest request)
        {
            if (request.TargetZoneId == ZoneId.PlayerBoard || request.TargetZoneId == ZoneId.EnemyBoard)
            {
                if (request.SourceCard.CurrentCardData.Tags != null && request.SourceCard.CurrentCardData.Tags.Contains(CardTag.Entity))
                {
                    Debug.Log($"[Rule] PlayEntityRule: 将实体 {request.SourceCard.CurrentCardData.CardName} 放入战场");
                    if (ZoneTransferService.MoveCard(request.SourceCard, request.TargetZone, true, request.SourceZone))
                    {
                        // 标记为已处理，防止后续默认的移动逻辑再次执行
                        request.IsHandled = true;
                    }
                }
            }
        }
    }
}