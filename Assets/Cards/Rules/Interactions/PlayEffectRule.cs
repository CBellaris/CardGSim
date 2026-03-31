using UnityEngine;
using Cards.Zones;
using Cards.Data;
using Cards.Core;
using Cards.Actions;

namespace Cards.Rules.Interactions
{
    /// <summary>
    /// 职责单一：验证并执行卡牌上配置的 Effect，收集产出的 Action 并统一入队。
    /// 不再负责卡牌去向（由 CardDispositionRule 处理）。
    /// </summary>
    public class PlayEffectRule : IInteractionRule
    {
        public int Priority => 15;

        public bool Validate(InteractionRequest request)
        {
            var effects = request.SourceCard.CurrentCardData.PlayEffects;
            if (effects == null || effects.Count == 0) return true;

            foreach (var effect in effects)
            {
                if (effect == null) continue;

                if (!effect.CanExecute(request, out string failureReason))
                {
                    Debug.LogWarning(failureReason);
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
            var effects = request.SourceCard.CurrentCardData.PlayEffects;
            if (effects == null || effects.Count == 0) return;

            Debug.Log($"[Rule] PlayEffectRule: 执行卡牌 {request.SourceCard.CurrentCardData.CardName} 的效果");

            foreach (var effect in effects)
            {
                if (effect == null) continue;

                var actions = effect.Execute(request);
                foreach (var action in actions)
                {
                    ActionManager.Instance.AddAction(action);
                }

                if (request.IsCancelled) return;
            }
        }
    }
}
