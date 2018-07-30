using log4net;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TurboConsole.Diagnostics
{
    public class TurboConsoleLog
    {
        /*

        Loggers may be assigned levels. 
        Levels are instances of the log4net.Core.Level class. 
        The following levels are defined in order of increasing priority:
         
        ALL
        DEBUG
        INFO
        WARN
        ERROR
        FATAL
        OFF

        */

        private static readonly String messagePrefix = String.Empty;

        private static readonly ILog Log;
        static TurboConsoleLog()
        {
            Log = LogManager.GetLogger("TurboConsole.Diagnostics");
            if (Sitecore.Configuration.Factory.GetConfigNode("log4net/appender[@name='TurboConsoleFileAppender']").IsNull())
            {
                messagePrefix = "[TConsole]";
            }
            if (Log.IsNull())
            {
                Log = LoggerFactory.GetLogger(typeof(TurboConsoleLog));
            }
        }

        public static void Audit(String message, params object[] parameters)
        {
            Assert.ArgumentNotNull(message, nameof(message));
            if (parameters?.Length > 0)
            {
                message = String.Format(message, parameters);
            }
            Log.Info($"{messagePrefix}AUDIT ({Sitecore.Context.User?.Name ?? "unknown user"}) {message}");
        }

        public static void Debug(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            Assert.ArgumentNotNull(message, "message");
            if (exception == null)
            {
                Log.Debug(FormatMessage(message));
            }
            else
            {
                Log.Debug(FormatMessage(message), exception);
            }
        }

        private static string FormatMessage(string message)
        {
            return messagePrefix != string.Empty ? messagePrefix + message : message;
        }

        public static void Error(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            if (exception == null)
            {
                Log.Error(FormatMessage(message));
            }
            else
            {
                Log.Error(FormatMessage(message), exception);
            }
        }

        public static void Fatal(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            if (exception == null)
            {
                Log.Fatal(FormatMessage(message));
            }
            else
            {
                Log.Fatal(FormatMessage(message), exception);
            }
        }

        public static void Info(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            if (exception == null)
            {
                Log.Info(FormatMessage(message));
            }
            else
            {
                Log.Info(FormatMessage(message), exception);
            }
        }

        public static void Warn(string message, Exception exception = null)
        {
            Assert.IsNotNull(Log, "Logger implementation was not initialized");
            if (exception == null)
            {
                Log.Warn(FormatMessage(message));
            }
            else
            {
                Log.Warn(FormatMessage(message), exception);
            }
        }
    }
}