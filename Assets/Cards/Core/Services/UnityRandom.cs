using Cards.Services;

namespace Cards.Core.Services
{
    public class UnityRandom : IRandom
    {
        public int Range(int min, int max) => UnityEngine.Random.Range(min, max);
    }
}
