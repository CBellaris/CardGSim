using UnityEngine;

namespace Cards.Core
{
    public enum AttackResult
    {
        CriticalMiss,
        Miss,
        Hit,
        CriticalHit
    }

    public struct AttackRollResult
    {
        public AttackResult Result;
        public int NaturalRoll;
        public int TotalAttack;
        public int TargetAC;
    }

    /// <summary>
    /// Centralized combat resolution service.
    /// All D20 attack rolls and damage calculations go through here
    /// to ensure consistency across DamageEffect, AttackRule, and future combat logic.
    /// </summary>
    public static class CombatResolver
    {
        public static AttackRollResult RollAttack(int attackBonus, int targetArmorClass)
        {
            int roll = Random.Range(1, 21);
            int totalAttack = roll + attackBonus;

            AttackResult result;
            if (roll == 20) result = AttackResult.CriticalHit;
            else if (roll == 1) result = AttackResult.CriticalMiss;
            else result = totalAttack >= targetArmorClass ? AttackResult.Hit : AttackResult.Miss;

            return new AttackRollResult
            {
                Result = result,
                NaturalRoll = roll,
                TotalAttack = totalAttack,
                TargetAC = targetArmorClass
            };
        }

        public static bool IsHit(AttackResult result)
        {
            return result == AttackResult.Hit || result == AttackResult.CriticalHit;
        }

        public static int RollDamage(int diceCount, int diceSides, int baseDamage = 0, bool isCrit = false)
        {
            if (diceCount <= 0 || diceSides <= 0)
            {
                return isCrit ? baseDamage * 2 : baseDamage;
            }

            int rolls = isCrit ? diceCount * 2 : diceCount;
            return DiceRoller.Roll(rolls, diceSides);
        }
    }
}
