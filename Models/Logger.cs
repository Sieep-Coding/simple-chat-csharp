using System;
using System.IO;
using System.Threading;

namespace CSharpStream.Models
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static StreamWriter? _fileWriter;
        private static string _logFilePath = string.Empty;

        public static void Init(string? logFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(logFilePath)) return;
            _logFilePath = logFilePath;
            _fileWriter = new StreamWriter(File.Open(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                AutoFlush = true
            };
        }

        public static void Info(string sender, string message) => Log("INFO", sender, message, ConsoleColor.Green);
        public static void Warn(string sender, string message) => Log("WARN", sender, message, ConsoleColor.Yellow);
        public static void Error(string sender, string message) => Log("ERROR", sender, message, ConsoleColor.Red);
        public static void Debug(string sender, string message) => Log("DEBUG", sender, message, ConsoleColor.Cyan);

        private static void Log(string level, string sender, string message, ConsoleColor color)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formatted = $"[{timestamp}] [{level}] [{sender}] {message}";

            lock (_lock)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(formatted);
                Console.ForegroundColor = oldColor;

                _fileWriter?.WriteLine(formatted);
            }
        }

        public static void Shutdown()
        {
            lock (_lock)
            {
                _fileWriter?.Dispose();
                _fileWriter = null;
            }
        }
    }
}