using System.Collections.Generic;
using Cards.Actions;
using Cards.Effects;

namespace Cards.Rules.Interactions
{
    public class PlayEffectRule : IInteractionRule
    {
        public int Priority => 15;

        public bool Validate(InteractionRequest request)
        {
            IReadOnlyList<ICardEffect> effects = request.SourceCard?.Data?.PlayEffects;
            if (effects == null || effects.Count == 0)
            {
                return true;
            }

            foreach (ICardEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                if (!effect.CanExecute(request, out string failureReason))
                {
                    request.Context?.Logger?.LogWarning(failureReason);
                    return false;
                }
            }

            return true;
        }

        public void BeforeExecute(InteractionRequest request)
        {
        }

        public void Execute(InteractionRequest request)
        {
            IReadOnlyList<ICardEffect> effects = request.SourceCard?.Data?.PlayEffects;
            if (effects == null || effects.Count == 0)
            {
                return;
            }

            request.Context?.Logger?.Log($"[Rule] PlayEffectRule: 执行卡牌 {request.SourceCard.Data?.CardName} 的效果");

            foreach (ICardEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                List<GameAction> actions = effect.Execute(request);
                if (actions == null)
                {
                    continue;
                }

                foreach (GameAction action in actions)
                {
                    request.Context?.Actions?.Enqueue(action);
                }

                if (request.IsCancelled)
                {
                    return;
                }
            }
        }
    }
}
