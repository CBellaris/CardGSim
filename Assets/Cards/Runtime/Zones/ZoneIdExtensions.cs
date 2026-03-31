namespace Cards.Zones
{
    public static class ZoneIdExtensions
    {
        public static bool IsBoard(this ZoneId zoneId)
        {
            return zoneId == ZoneId.PlayerBoard || zoneId == ZoneId.EnemyBoard;
        }
    }
}
