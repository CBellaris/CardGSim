using Cards.Zones;

namespace Cards.Core
{
    public enum CardOwner
    {
        Neutral,
        Player,
        Enemy
    }

    public static class CardOwnerExtensions
    {
        public static CardOwner GetOpponent(this CardOwner owner)
        {
            switch (owner)
            {
                case CardOwner.Player:
                    return CardOwner.Enemy;
                case CardOwner.Enemy:
                    return CardOwner.Player;
                default:
                    return CardOwner.Neutral;
            }
        }

        public static ZoneId GetBoardZoneId(this CardOwner owner)
        {
            return owner == CardOwner.Enemy ? ZoneId.EnemyBoard : ZoneId.PlayerBoard;
        }
    }
}
