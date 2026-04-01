using System.Linq;
using Cards.Core.Events;
using Cards.Data;
using Cards.Zones;

namespace Cards.Rules.Interactions
{
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
            if (request.Type != InteractionType.PlayCard || request.SourceZoneId != ZoneId.PlayerHand)
            {
                return;
            }

            var tags = request.SourceCard?.Data?.Tags;
            bool isEntity = tags != null && tags.Contains(CardTag.Entity);
            if (isEntity)
            {
                return;
            }

            request.SourceZone?.RemoveCard(request.SourceCard);

            ZoneId targetZoneId = tags != null && tags.Contains(CardTag.Exhaust)
                ? ZoneId.PlayerExhaustPile
                : ZoneId.PlayerDiscardPile;

            request.Context?.Events?.Publish(new RequestMoveCardEvent
            {
                Card = request.SourceCard,
                SourceZone = request.SourceZone,
                SourceZoneId = request.SourceZoneId,
                TargetZoneId = targetZoneId
            });

            request.IsHandled = true;
        }
    }
}
