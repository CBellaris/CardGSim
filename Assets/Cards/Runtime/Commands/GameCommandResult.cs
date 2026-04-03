using System;
using System.Collections.Generic;
using Cards.Core;
using Cards.Rules.Interactions;

namespace Cards.Commands
{
    public enum GameCommandStatus
    {
        Executed,
        Rejected,
        AwaitingTargetSelection
    }

    public sealed class GameCommandResult
    {
        private static readonly IReadOnlyList<CardInstance> EmptyTargets = Array.Empty<CardInstance>();

        private GameCommandResult(
            GameCommandStatus status,
            string message,
            InteractionRequest interaction,
            IReadOnlyList<CardInstance> availableTargets)
        {
            Status = status;
            Message = message;
            Interaction = interaction;
            AvailableTargets = availableTargets ?? EmptyTargets;
        }

        public GameCommandStatus Status { get; }
        public string Message { get; }
        public InteractionRequest Interaction { get; }
        public IReadOnlyList<CardInstance> AvailableTargets { get; }

        public bool Succeeded => Status == GameCommandStatus.Executed;
        public bool RequiresTargetSelection => Status == GameCommandStatus.AwaitingTargetSelection;

        public static GameCommandResult Executed(string message = null, InteractionRequest interaction = null)
        {
            return new GameCommandResult(GameCommandStatus.Executed, message, interaction, EmptyTargets);
        }

        public static GameCommandResult Rejected(string message)
        {
            return new GameCommandResult(GameCommandStatus.Rejected, message, null, EmptyTargets);
        }

        public static GameCommandResult AwaitingTargetSelection(
            string message,
            IReadOnlyList<CardInstance> availableTargets)
        {
            return new GameCommandResult(
                GameCommandStatus.AwaitingTargetSelection,
                message,
                null,
                availableTargets);
        }
    }
}
