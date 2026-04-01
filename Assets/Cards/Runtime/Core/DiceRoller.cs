using Cards.Services;

namespace Cards.Core
{
    public class DiceRoller
    {
        private readonly IRandom _random;

        public DiceRoller(IRandom random)
        {
            _random = random;
        }

        public int Roll(int diceCount, int diceSides)
        {
            int total = 0;
            for (int i = 0; i < diceCount; i++)
            {
                total += _random.Range(1, diceSides + 1);
            }
            return total;
        }
    }
}
