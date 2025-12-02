using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired
{
    public static class DebugLogger
    {
        public static void Log(string message)
        {
            if (Plugin.Instance.DevMode)
            {
                Console.WriteLine($"[Wired] {message}");
            }
        }
        public static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Wired][ERROR] {message}");
            Console.ResetColor();
        }
        public static void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Wired][WARNING] {message}");
            Console.ResetColor();
        }
    }
}
