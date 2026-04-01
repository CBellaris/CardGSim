namespace Cards.Services
{
    public enum GameInputAction
    {
        DrawCard,
        EndTurn
    }

    public interface IInputProvider
    {
        bool WasPressed(GameInputAction action);
    }
}
