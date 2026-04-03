using Cards.Core;

namespace Cards.Commands
{
    public enum TargetSelectionMode
    {
        Auto,
        Explicit,
        Deferred
    }

    public struct CommandTargetSelection
    {
        private CommandTargetSelection(TargetSelectionMode mode, CardInstance explicitTarget)
        {
            Mode = mode;
            ExplicitTarget = explicitTarget;
        }

        public TargetSelectionMode Mode { get; }
        public CardInstance ExplicitTarget { get; }

        public static CommandTargetSelection Auto()
        {
            return new CommandTargetSelection(TargetSelectionMode.Auto, null);
        }

        public static CommandTargetSelection Explicit(CardInstance target)
        {
            return new CommandTargetSelection(TargetSelectionMode.Explicit, target);
        }

        public static CommandTargetSelection Deferred()
        {
            return new CommandTargetSelection(TargetSelectionMode.Deferred, null);
        }
    }
}
