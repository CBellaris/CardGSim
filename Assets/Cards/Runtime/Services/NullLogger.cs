namespace Cards.Services
{
    public class NullLogger : ILogger
    {
        public void Log(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }
}
