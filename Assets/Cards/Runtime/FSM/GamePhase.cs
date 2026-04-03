namespace Cards.FSM
{
    public enum GamePhase
    {
        None,
        GameSetup,
        PlayerTurnStart,
        PlayerMainPhase,
        PlayerTurnEnd,
        EnemyTurn
    }
}
