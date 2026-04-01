using System.Collections.Generic;
using Cards.Actions;
using Cards.Rules.Interactions;

namespace Cards.Effects
{
    public interface ICardEffect
    {
        bool CanExecute(InteractionRequest request, out string failureReason);
        List<GameAction> Execute(InteractionRequest request);
    }
}
