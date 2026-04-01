using UnityEngine;
using Cards.Services;

namespace Cards.Core.Services
{
    public class UnityTimeProvider : ITimeProvider
    {
        public float DeltaTime => Time.deltaTime;
    }
}
