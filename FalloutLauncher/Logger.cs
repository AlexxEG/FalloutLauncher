using System;
using System.IO;

namespace FalloutLauncher
{
    public class Logger
    {
        public string Prefix => $"[{DateTime.Now}] ";

        private StreamWriter _log;

        public Logger(string path)
        {
            _log = new StreamWriter(path, true) { AutoFlush = true };
        }

        public void Close()
        {
            _log.Flush();
            _log.Close();
            _log = null;
        }

        /// <summary>
        /// Writes message to both log and console window.
        /// </summary>
        public void WriteAndLogLine(string message, params object[] args)
        {
            _log.WriteLine(Prefix + message, args);
            Console.WriteLine(message, args);
        }

        /// <summary>
        /// Writes message to both log and console window.
        /// </summary>
        public void WriteAndLogLine(object value)
        {
            _log.WriteLine(Prefix + value);
            Console.WriteLine(value);
        }

        /// <summary>
        /// Writes a string to log and console window.
        /// </summary>
        public void WriteAndLog(string value)
        {
            _log.Write(value);
            Console.Write(value);
        }

        /// <summary>
        /// Writes a character to log and console window.
        /// </summary>
        public void WriteAndLog(char c)
        {
            _log.Write(c);
            Console.Write(c);
        }

        /// <summary>
        /// Writes to log only.
        /// </summary>
        public void LogLine(string message, params object[] args)
        {
            _log.WriteLine(Prefix + message, args);
        }

        /// <summary>
        /// Writes to log only.
        /// </summary>
        public void LogLine(object value, bool noPrefix)
        {
            _log.WriteLine((noPrefix ? string.Empty : Prefix) + value);
        }

        /// <summary>
        /// Writes to log only.
        /// </summary>
        public void LogLine(object value)
        {
            _log.WriteLine(Prefix + value);
        }

        /// <summary>
        /// Writes line terminator to log.
        /// </summary>
        public void LogLine()
        {
            _log.WriteLine();
        }

        /// <summary>
        /// Writes a string to log and console window.
        /// </summary>
        public void Log(string value)
        {
            _log.Write(value);
        }
    }
}
