namespace Cards.FSM
{
    public sealed class GameSessionOptions
    {
        public static GameSessionOptions Default { get; } = new GameSessionOptions();

        public int InitialHandSize { get; set; } = 3;
        public int TurnStartDrawCount { get; set; } = 1;
        public float EnemyTurnDelaySeconds { get; set; } = 1.5f;
    }
}
