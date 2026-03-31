using UnityEngine;

namespace Cards.Core
{
    public class DiceRoller
    {
        public static int Roll(int diceCount, int diceSides)
        {
            int total = 0;
            for (int i = 0; i < diceCount; i++)
            {
                total += Random.Range(1, diceSides + 1);
            }
            return total;
        }
    }
}
