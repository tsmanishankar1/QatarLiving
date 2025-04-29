using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.EventLogger
{
    public interface IEventlogger
    {
        void LogException(Exception ex);
        void LogTrace(string message);
        void LogError(string message);
    }
}
