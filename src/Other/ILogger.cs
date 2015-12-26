using System;

namespace DnxMigrater.Other
{
    public interface ILogger
    {
        void Log(string message, params object[] args);
        void Trace(string message, params object[] args);
        void Debug(string message, params object[] args);
        void Info(string message, params object[] args);

        void Error(string message, params object[] args);
        void Error(Exception ex, string message, params object[] args);

        void ConditionalTrace(string message, params object[] args);
        void ConditionalDebug(string message, params object[] args);
    }
}