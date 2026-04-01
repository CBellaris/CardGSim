using System;
using System.Collections.Generic;
using UnityEngine;
using Cards.Rules.Interactions;
using Cards.Actions;
using Cards.Core;
namespace Cards.Effects
{
    [Serializable]
    public class DamageEffect : ICardEffect
    {
        [Header("Damage Configuration")]
        [Tooltip("If true, uses D&D d20 attack roll against target's Armor Class. If false, deals direct damage.")]
        public bool useAttackRoll = true;

        [Tooltip("Base damage or fallback attack value.")]
        public int attackValue;
        
        [Tooltip("Number of dice to roll for damage.")]
        public int diceCount;
        
        [Tooltip("Number of sides on the damage dice.")]
        public int diceSides;

        [Tooltip("Bonus to add to the d20 attack roll.")]
        public int attackBonus;

        public bool CanExecute(InteractionRequest request, out string failureReason)
        {
            if (request == null || request.SourceCard == null)
            {
                failureReason = "[DamageEffect] 缺少有效的出牌上下文。";
                return false;
            }

            if (request.TargetEntity == null)
            {
                failureReason = "[DamageEffect] 无法执行，因为没有目标实体。";
                return false;
            }

            failureReason = null;
            return true;
        }

        public List<GameAction> Execute(InteractionRequest request)
        {
            var actions = new List<GameAction>();

            if (!CanExecute(request, out string failureReason))
            {
                Debug.LogWarning(failureReason);
                return actions;
            }

            ICombatable targetModel = request.TargetEntity;
            string sourceName = request.SourceCard.Data?.CardName ?? "Unknown";
            string targetName = targetModel.CombatName;

            CombatResolver combat = request.Context?.Combat;
            if (combat == null)
            {
                Debug.LogWarning("[DamageEffect] 缺少 CombatResolver，无法执行伤害效果。");
                return actions;
            }

            if (useAttackRoll)
            {
                var roll = combat.RollAttack(attackBonus, targetModel.ArmorClass);
                Debug.Log($"[Effect] {sourceName} attacks {targetName}! Roll: {roll.NaturalRoll} + {attackBonus} = {roll.TotalAttack} vs AC {roll.TargetAC}");

                if (CombatResolver.IsHit(roll.Result))
                {
                    bool isCrit = roll.Result == AttackResult.CriticalHit;
                    int damage = combat.RollDamage(diceCount, diceSides, attackValue, isCrit);
                    Debug.Log($"[Effect] Hit{(isCrit ? " (CRITICAL)" : "")}! Damage: {damage}");
                    actions.Add(new DamageAction(targetModel, damage, sourceName));
                }
                else
                {
                    Debug.Log($"[Effect] Miss!");
                }
            }
            else
            {
                int damage = combat.RollDamage(diceCount, diceSides, attackValue, false);
                Debug.Log($"[Effect] Direct Hit! Damage: {damage}");
                actions.Add(new DamageAction(targetModel, damage, sourceName));
            }

            return actions;
        }
    }
}
