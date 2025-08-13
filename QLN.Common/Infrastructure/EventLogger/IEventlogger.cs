
namespace QLN.Common.Infrastructure.EventLogger
{
    public interface IEventlogger
    {
        void LogException(Exception ex);
        void LogTrace(string message);
        void LogError(string message);
        void LogError(Exception ex, string message, long id);
        void LogWarning(string message, long id);
    }
}
