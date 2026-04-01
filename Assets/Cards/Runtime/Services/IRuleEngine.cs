using Cards.Rules.Interactions;

namespace Cards.Services
{
    public interface IRuleEngine
    {
        void ProcessInteraction(InteractionRequest request);
    }
}
