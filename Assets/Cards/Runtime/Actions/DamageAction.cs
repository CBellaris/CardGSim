using Cards.Core;
using Cards.Core.Events;
using Cards.Services;

namespace Cards.Actions
{
    public class DamageAction : GameAction
    {
        private readonly ICombatable target;
        private readonly int damage;
        private readonly string sourceName;

        public DamageAction(ICombatable target, int damage, string sourceName = "Unknown")
        {
            this.target = target;
            this.damage = damage;
            this.sourceName = sourceName;
        }

        public override void Execute(GameContext ctx)
        {
            target?.TakeDamage(damage);
            ctx?.Logger?.Log($"[Action] {sourceName} dealt {damage} to {target?.CombatName ?? "Unknown"}");

            if (target is CardInstance card && card.Model != null && card.Model.CurrentHealth <= 0)
            {
                ctx?.Events?.Publish(new CardDiedEvent { Card = card });
            }
        }
    }
}
