using System;
using Cards.Core;

namespace Cards.Commands
{
    public sealed class PlayCardCommand
    {
        public PlayCardCommand(CardInstance card, CommandTargetSelection targetSelection = default)
        {
            Card = card ?? throw new ArgumentNullException(nameof(card));
            TargetSelection = targetSelection;
        }

        public CardInstance Card { get; }
        public CommandTargetSelection TargetSelection { get; }
    }
}
