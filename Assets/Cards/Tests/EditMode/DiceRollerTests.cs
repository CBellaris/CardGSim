using NUnit.Framework;
using Cards.Core;

namespace Cards.Tests.EditMode
{
    public class DiceRollerTests
    {
        [Test]
        public void Roll_2d6_ReturnsSumOfTwoRolls()
        {
            var random = new MockRandom(3, 5);
            var roller = new DiceRoller(random);

            int result = roller.Roll(2, 6);

            Assert.AreEqual(8, result);
        }

        [Test]
        public void Roll_1d20_ReturnsSingleRoll()
        {
            var random = new MockRandom(15);
            var roller = new DiceRoller(random);

            int result = roller.Roll(1, 20);

            Assert.AreEqual(15, result);
        }

        [Test]
        public void Roll_ZeroDice_ReturnsZero()
        {
            var random = new MockRandom();
            var roller = new DiceRoller(random);

            int result = roller.Roll(0, 6);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void Roll_3d8_ReturnsSumOfThreeRolls()
        {
            var random = new MockRandom(2, 7, 4);
            var roller = new DiceRoller(random);

            int result = roller.Roll(3, 8);

            Assert.AreEqual(13, result);
        }
    }
}
