using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.EventLogger
{
    public class Eventlogger : IEventlogger
    {
        private readonly ILogger<Eventlogger> _logger;
        public Eventlogger(ILogger<Eventlogger> logger)
        {
            _logger = logger;
        }
        private string _WriteSingleException(Exception ex, int level)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.Append("-----------------------------------------------------------------------\r\n ");
            if (level != 0)
            {
                stringBuilder.AppendFormat("Inner exception: {0}\r\n", level);
            }

            stringBuilder.AppendFormat("Type: {0}\r\n", ex.GetType().FullName);
            stringBuilder.AppendFormat("Message: {0}\r\n", ex.Message.ToString());
            stringBuilder.AppendFormat("Source: {0}\r\n", ex.Source);
            stringBuilder.AppendFormat("Helplink: {0}\r\n", ex.HelpLink);
            stringBuilder.AppendFormat("TargetSite: {0}\r\n", ex.TargetSite);
            int hRForException = Marshal.GetHRForException(ex);
            stringBuilder.AppendFormat("HRResult: {0}\r\n", hRForException);
            stringBuilder.AppendFormat("Data: {0}\r\n", ex.Data.GetType().Name);
            foreach (DictionaryEntry datum in ex.Data)
            {
                stringBuilder.AppendFormat("{0}={1}\r\n", datum.Key, datum.Value);
            }

            if (ex.StackTrace == null)
            {
                stringBuilder.Append("Stack Trace: Not available\r\n");
            }
            else
            {
                stringBuilder.AppendFormat("Stack Trace: {0}\r\n", ex.StackTrace.ToString());
            }

            return stringBuilder.ToString();
        }

        private string _FormatException(Exception ex)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            stringBuilder.AppendFormat("Timestamp: {0:yyyy/MM/dd HH:mm:ss.ttt}\r\n", DateTime.Now);
            stringBuilder.AppendFormat("MessageHandlingInstanceId: {0}\r\n", Guid.NewGuid());
            stringBuilder.AppendFormat("An exception of type '{0}' occurred and was caught.\r\n", ex.GetType().AssemblyQualifiedName);
            stringBuilder.Append(_WriteSingleException(ex, 0));
            ex = ex.InnerException;
            int num = 1;
            while (ex != null)
            {
                stringBuilder.Append(_WriteSingleException(ex, num++));
                ex = ex.InnerException;
            }

            stringBuilder.Append("-----------------------------------------------------------------------\r\n ");
            stringBuilder.AppendFormat("Machine: {0}\r\n", Environment.MachineName);
            stringBuilder.AppendFormat("Assembly: {0}\r\n", Assembly.GetCallingAssembly().FullName);
            stringBuilder.AppendFormat("AppDomainName: {0}\r\n", AppDomain.CurrentDomain.FriendlyName);
            stringBuilder.AppendFormat("ThreadName: {0}\r\n", Thread.CurrentThread.Name);
            stringBuilder.AppendFormat("ThreadIdentity: {0}\r\n", Thread.CurrentThread.ManagedThreadId);
            stringBuilder.AppendFormat("WindowsIdentity: {0}\r\n", Environment.UserDomainName);
            stringBuilder.AppendFormat("ProcessID: {0}\r\n", Process.GetCurrentProcess().Id);
            return stringBuilder.ToString();
        }
        public void LogException(Exception ex)
        {
            _logger.LogError(_FormatException(ex));
        }
        public void LogTrace(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogError(string message)
        {
            _logger.LogError(message);
        }
    }
}
