using System;
using NLog;

namespace DnxMigrater.Other
{

    public class NLogLogger : ILogger
    {
        private Logger _log;

        public NLogLogger(Logger l)
        {
            _log = l;
        }

        public void Log(string message, params object[] args)
        {
            _log.Log(LogLevel.Info, message, args);
        }

        public void Trace(string message, params object[] args)
        {
            _log.Trace(message, args);
        }

        public void Debug(string message, params object[] args)
        {
            _log.Debug(message, args);
        }

        public void Info(string message, params object[] args)
        {
            _log.Info(message, args);
        }

        public void Warn(string message, params object[] args)
        {
            _log.Warn(message,args);
        }

        public void Error(string message, params object[] args)
        {
            _log.Error(message, args);
        }

        public void Error(Exception ex, string message, params object[] args)
        {
            _log.Error(ex, message, args);
        }

        public void ConditionalTrace(string message, params object[] args)
        {
            _log.ConditionalTrace(message, args);
        }

        public void ConditionalDebug(string message, params object[] args)
        {
            _log.ConditionalDebug(message, args);
        }
    }
}