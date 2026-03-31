using UnityEngine;
using Cards.Data;
using Cards.Zones;
using Cards.Core.Events;

namespace Cards.Rules.Interactions
{
    /// <summary>
    /// 处理非实体牌（Action/Spell）打出后的去向：弃牌堆或消耗堆。
    /// 优先级最低，确保在 PlayEffectRule 和 PlayEntityRule 之后执行。
    /// 如果 PlayEntityRule 已经将实体牌放入战场并设置了 IsHandled，本规则不会执行。
    /// </summary>
    public class CardDispositionRule : IInteractionRule
    {
        public int Priority => 5;

        public bool Validate(InteractionRequest request)
        {
            return true;
        }

        public void BeforeExecute(InteractionRequest request)
        {
        }

        public void Execute(InteractionRequest request)
        {
            if (request.Type != InteractionType.PlayCard) return;
            if (request.SourceZoneId != ZoneId.PlayerHand) return;

            var tags = request.SourceCard.CurrentCardData.Tags;
            bool isEntity = tags != null && tags.Contains(CardTag.Entity);
            if (isEntity) return;

            if (request.SourceZone != null)
            {
                request.SourceZone.RemoveCard(request.SourceCard, true);
            }

            ZoneId targetZoneId = (tags != null && tags.Contains(CardTag.Exhaust))
                ? ZoneId.PlayerExhaustPile
                : ZoneId.PlayerDiscardPile;

            EventBus.Publish(new RequestMoveCardEvent
            {
                Card = request.SourceCard,
                SourceZone = request.SourceZone,
                SourceZoneId = request.SourceZoneId,
                TargetZoneId = targetZoneId,
                UseAnimation = true
            });

            request.IsHandled = true;
        }
    }
}
