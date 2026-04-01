using Cards.Actions;

namespace Cards.Services
{
    public interface IActionQueue
    {
        void Enqueue(GameAction action);
        bool IsProcessing { get; }
    }
}
