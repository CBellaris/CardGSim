using Cards.Services;

namespace Cards.FSM
{
    public interface IGameSessionBootstrap
    {
        bool Initialize(GameContext context);
    }
}
