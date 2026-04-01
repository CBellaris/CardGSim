namespace Cards.Services
{
    public interface IAnimationPolicy
    {
        bool IsEnabled { get; }
        float TimeScale { get; }
    }
}
