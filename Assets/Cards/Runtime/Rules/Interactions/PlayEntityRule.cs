using System.Linq;
using Cards.Data;
using Cards.Zones;

namespace Cards.Rules.Interactions
{
    public class PlayEntityRule : IInteractionRule
    {
        public int Priority => 10;

        public bool Validate(InteractionRequest request)
        {
            if (request.TargetZoneId != ZoneId.PlayerBoard && request.TargetZoneId != ZoneId.EnemyBoard)
            {
                return true;
            }

            if (request.TargetZone == null)
            {
                return false;
            }

            return true;
        }

        public void BeforeExecute(InteractionRequest request)
        {
        }

        public void Execute(InteractionRequest request)
        {
            if (request.TargetZoneId != ZoneId.PlayerBoard && request.TargetZoneId != ZoneId.EnemyBoard)
            {
                return;
            }

            var tags = request.SourceCard?.Data?.Tags;
            bool isEntity = tags != null && tags.Contains(CardTag.Entity);
            if (!isEntity)
            {
                return;
            }

            request.Context?.Logger?.Log($"[Rule] PlayEntityRule: 将实体 {request.SourceCard.Data?.CardName} 放入战场");
            if (request.Context?.ZoneTransfers?.MoveCard(request.SourceCard, request.TargetZone, request.SourceZone) == true)
            {
                request.IsHandled = true;
            }
        }
    }
}
