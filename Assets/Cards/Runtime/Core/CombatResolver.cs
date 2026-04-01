using Cards.Services;

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
    /// 战斗结算服务（实例类）。
    /// 所有 D20 攻击检定和伤害计算统一经由此类，确保 DamageEffect、AttackRule 等调用者行为一致。
    /// </summary>
    public class CombatResolver
    {
        private readonly IRandom _random;
        private readonly DiceRoller _dice;

        public CombatResolver(IRandom random, DiceRoller dice)
        {
            _random = random;
            _dice = dice;
        }

        public AttackRollResult RollAttack(int attackBonus, int targetArmorClass)
        {
            int roll = _random.Range(1, 21);
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

        public int RollDamage(int diceCount, int diceSides, int baseDamage = 0, bool isCrit = false)
        {
            if (diceCount <= 0 || diceSides <= 0)
            {
                return isCrit ? baseDamage * 2 : baseDamage;
            }

            int rolls = isCrit ? diceCount * 2 : diceCount;
            return _dice.Roll(rolls, diceSides);
        }
    }
}
