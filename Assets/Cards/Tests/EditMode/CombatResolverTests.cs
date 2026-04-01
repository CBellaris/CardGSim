using NUnit.Framework;
using Cards.Core;

namespace Cards.Tests.EditMode
{
    public class CombatResolverTests
    {
        [Test]
        public void RollAttack_Natural20_CriticalHit()
        {
            var random = new MockRandom(20);
            var dice = new DiceRoller(random);
            var combat = new CombatResolver(random, dice);

            var result = combat.RollAttack(0, 25);

            Assert.AreEqual(AttackResult.CriticalHit, result.Result);
            Assert.AreEqual(20, result.NaturalRoll);
        }

        [Test]
        public void RollAttack_Natural1_CriticalMiss()
        {
            var random = new MockRandom(1);
            var dice = new DiceRoller(random);
            var combat = new CombatResolver(random, dice);

            var result = combat.RollAttack(10, 5);

            Assert.AreEqual(AttackResult.CriticalMiss, result.Result);
            Assert.AreEqual(1, result.NaturalRoll);
        }

        [Test]
        public void RollAttack_TotalMeetsAC_Hit()
        {
            var random = new MockRandom(10);
            var dice = new DiceRoller(random);
            var combat = new CombatResolver(random, dice);

            var result = combat.RollAttack(5, 15); // 10 + 5 = 15 >= AC 15

            Assert.AreEqual(AttackResult.Hit, result.Result);
            Assert.AreEqual(15, result.TotalAttack);
        }

        [Test]
        public void RollAttack_TotalBelowAC_Miss()
        {
            var random = new MockRandom(10);
            var dice = new DiceRoller(random);
            var combat = new CombatResolver(random, dice);

            var result = combat.RollAttack(3, 15); // 10 + 3 = 13 < AC 15

            Assert.AreEqual(AttackResult.Miss, result.Result);
            Assert.AreEqual(13, result.TotalAttack);
        }

        [Test]
        public void IsHit_HitAndCriticalHit_ReturnsTrue()
        {
            Assert.IsTrue(CombatResolver.IsHit(AttackResult.Hit));
            Assert.IsTrue(CombatResolver.IsHit(AttackResult.CriticalHit));
        }

        [Test]
        public void IsHit_MissAndCriticalMiss_ReturnsFalse()
        {
            Assert.IsFalse(CombatResolver.IsHit(AttackResult.Miss));
            Assert.IsFalse(CombatResolver.IsHit(AttackResult.CriticalMiss));
        }

        [Test]
        public void RollDamage_2d6_ReturnsDiceSum()
        {
            var random = new MockRandom(4, 6);
            var dice = new DiceRoller(random);
            var combat = new CombatResolver(random, dice);

            int damage = combat.RollDamage(2, 6);

            Assert.AreEqual(10, damage);
        }

        [Test]
        public void RollDamage_Crit_DoublesNumberOfDice()
        {
            var random = new MockRandom(3, 3, 3, 3);
            var dice = new DiceRoller(random);
            var combat = new CombatResolver(random, dice);

            int damage = combat.RollDamage(2, 6, 0, true); // crit → 4d6

            Assert.AreEqual(12, damage);
        }

        [Test]
        public void RollDamage_NoDice_ReturnsBaseDamage()
        {
            var random = new MockRandom();
            var dice = new DiceRoller(random);
            var combat = new CombatResolver(random, dice);

            int damage = combat.RollDamage(0, 0, 5);

            Assert.AreEqual(5, damage);
        }

        [Test]
        public void RollDamage_NoDice_Crit_DoublesBaseDamage()
        {
            var random = new MockRandom();
            var dice = new DiceRoller(random);
            var combat = new CombatResolver(random, dice);

            int damage = combat.RollDamage(0, 0, 5, true);

            Assert.AreEqual(10, damage);
        }
    }
}
