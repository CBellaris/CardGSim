using System.Collections.Generic;
using Cards.Services;

namespace Cards.Tests
{
    public class MockRandom : IRandom
    {
        private readonly Queue<int> _sequence;

        public MockRandom(params int[] values)
        {
            _sequence = new Queue<int>(values);
        }

        public int Range(int min, int max) => _sequence.Dequeue();
    }
}
