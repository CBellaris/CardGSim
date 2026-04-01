using Cards.Actions;
using Cards.Core;

namespace Cards.Rules.Interactions
{
    public class AttackRule : IInteractionRule
    {
        public int Priority => 20;

        public bool Validate(InteractionRequest request)
        {
            if (request.Type == InteractionType.Attack && request.TargetEntity == null)
            {
                request.Context?.Logger?.LogWarning("[Rule] AttackRule: 实体攻击必须有目标！");
                return false;
            }

            return true;
        }

        public void BeforeExecute(InteractionRequest request)
        {
        }

        public void Execute(InteractionRequest request)
        {
            if (request.Type != InteractionType.Attack || request.TargetEntity == null)
            {
                return;
            }

            request.Context?.Logger?.Log(
                $"[Rule] AttackRule: 触发实体间攻击判定 {request.SourceCard.Data?.CardName} -> {request.TargetEntity.Data?.CardName}");

            ResolveAttack(request);
            request.IsHandled = true;
        }

        private static void ResolveAttack(InteractionRequest request)
        {
            CardInstance attacker = request.SourceCard;
            CardInstance target = request.TargetEntity;
            CombatResolver combat = request.Context?.Combat;

            if (attacker == null || target == null || combat == null)
            {
                return;
            }

            AttackRollResult roll = combat.RollAttack(attacker.AttackBonus, target.ArmorClass);
            request.Context.Logger?.Log(
                $"[Rule] {attacker.CombatName} attacks {target.CombatName}! Roll: {roll.NaturalRoll} + {attacker.AttackBonus} = {roll.TotalAttack} vs AC {roll.TargetAC}");

            if (!CombatResolver.IsHit(roll.Result))
            {
                request.Context.Logger?.Log("[Rule] Miss!");
                return;
            }

            bool isCrit = roll.Result == AttackResult.CriticalHit;
            int damage = combat.RollDamage(attacker.DiceCount, attacker.DiceSides, attacker.Attack, isCrit);
            request.Context.Logger?.Log($"[Rule] Hit{(isCrit ? " (CRITICAL)" : string.Empty)}! Queuing {damage} damage.");
            request.Context.Actions?.Enqueue(new DamageAction(target, damage, attacker.CombatName));
        }
    }
}
