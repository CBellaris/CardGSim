using Cards.Services;

namespace Cards.Core.Services
{
    public class UnityLogger : ILogger
    {
        public void Log(string msg) => UnityEngine.Debug.Log(msg);
        public void LogWarning(string msg) => UnityEngine.Debug.LogWarning(msg);
        public void LogError(string msg) => UnityEngine.Debug.LogError(msg);
    }
}
