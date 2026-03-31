using Cards.Core;

namespace Cards.Zones
{
    /// <summary>
    /// 统一处理卡牌在区域之间的转移，避免不同入口各自维护一套移入移出逻辑。
    /// </summary>
    public static class ZoneTransferService
    {
        public static bool MoveCard(CardEntity card, CardZone targetZone, bool useAnimation = true, CardZone sourceZone = null)
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

            resolvedSourceZone?.RemoveCard(card, useAnimation);
            targetZone.AddCard(card, useAnimation);
            return true;
        }
    }
}
