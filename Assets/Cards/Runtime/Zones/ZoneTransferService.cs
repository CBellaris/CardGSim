using Cards.Core;

namespace Cards.Zones
{
    public class ZoneTransferService
    {
        public bool MoveCard(CardInstance card, CardZone targetZone, CardZone sourceZone = null)
        {
            if (card == null || targetZone == null)
            {
                return false;
            }

            CardZone resolvedSourceZone = sourceZone ?? card.CurrentZone;
            if (resolvedSourceZone == targetZone)
            {
                return false;
            }

            resolvedSourceZone?.RemoveCard(card);
            targetZone.AddCard(card);
            return true;
        }
    }
}
