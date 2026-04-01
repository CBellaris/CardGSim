using Cards.Services;

namespace Cards.Core.Services
{
    public class LiveAnimationPolicy : IAnimationPolicy
    {
        public bool IsEnabled => true;
        public float TimeScale => 1.0f;
    }

    public class InstantAnimationPolicy : IAnimationPolicy
    {
        public bool IsEnabled => false;
        public float TimeScale => 0f;
    }
}
