using UnityEngine;
using Cards.Actions;
using Cards.Core;

namespace Cards.Rules.Interactions
{
    /// <summary>
    /// 仅处理场上实体之间的直接物理攻击 (InteractionType.Attack)
    /// 法术/行动牌的伤害已由 PlayEffectRule 和 DamageEffect 接管
    /// </summary>
    public class AttackRule : IInteractionRule
    {
        public int Priority => 20;

        public bool Validate(InteractionRequest request)
        {
            if (request.Type == InteractionType.Attack)
            {
                if (request.TargetEntity == null)
                {
                    Debug.LogWarning("[Rule] AttackRule: 实体攻击必须有目标！");
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
            if (request.Type == InteractionType.Attack)
            {
                if (request.TargetEntity != null)
                {
                    Debug.Log($"[Rule] AttackRule: 触发实体间攻击判定 {request.SourceCard.CurrentCardData.CardName} -> {request.TargetEntity.CurrentCardData.CardName}");
                    
                    ResolveAttack(request.SourceCard, request.TargetEntity);

                    request.IsHandled = true;
                }
            }
        }

        private void ResolveAttack(CardEntity attacker, CardEntity target)
        {
            if (target == null || attacker == null) return;

            ICombatable attackerModel = attacker.Model;
            ICombatable targetModel = target.Model;

            var roll = CombatResolver.RollAttack(attackerModel.AttackBonus, targetModel.ArmorClass);
            Debug.Log($"[Rule] {attackerModel.CombatName} attacks {targetModel.CombatName}! Roll: {roll.NaturalRoll} + {attackerModel.AttackBonus} = {roll.TotalAttack} vs AC {roll.TargetAC}");

            if (CombatResolver.IsHit(roll.Result))
            {
                bool isCrit = roll.Result == AttackResult.CriticalHit;
                int damage = CombatResolver.RollDamage(attackerModel.DiceCount, attackerModel.DiceSides, attackerModel.Attack, isCrit);
                Debug.Log($"[Rule] Hit{(isCrit ? " (CRITICAL)" : "")}! Queuing {damage} damage.");
                ActionManager.Instance.AddAction(new DamageAction(targetModel, damage, attackerModel.CombatName));
            }
            else
            {
                Debug.Log($"[Rule] Miss!");
            }
        }
    }
}
