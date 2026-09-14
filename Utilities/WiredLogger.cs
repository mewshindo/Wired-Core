using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Wired.Utilities
{
    public class WiredLogger
    {
        public static void Info(string message)
        {
            if (!Plugin.Instance.Configuration.LogDebugMessages) return;
            Logger.Log($"[Wired]: {message}");
        }
        public static void Warn(string message)
        {
            Logger.LogWarning($"[Wired] WARN: {message}");
        }
        public static void Error(string message)
        {
            Logger.LogError($"[Wired] ERROR: {message}");
        }

        public static void LogPluginLoaded(bool success)
        {
            if (success)
            {
                Console.WriteLine("           _              _ ", ConsoleColor.Yellow);
                Console.WriteLine("          (_)            | |", ConsoleColor.Yellow);
                Console.WriteLine(" __      ___ _ __ ___  __| |", ConsoleColor.Yellow);
                Console.WriteLine($" \\ \\ /\\ / / | '__/ _ \\/ _` |        Wired has loaded succesfully!", ConsoleColor.Yellow);
                Console.WriteLine("  \\ V  V /| | | |  __/ (_| |", ConsoleColor.Yellow);
                Console.WriteLine("   \\_/\\_/ |_|_|  \\___|\\__,_|", ConsoleColor.Yellow);
                Console.WriteLine(" ");
                Console.WriteLine($"Wired Version: {Assembly.GetAssembly(typeof(Plugin)).GetName().Version}");
                Console.WriteLine(" ");
                return;
            }

            Console.WriteLine("##########################################################", ConsoleColor.Red);
            Console.WriteLine("##########################################################", ConsoleColor.Red);
            Console.WriteLine("");
            Console.WriteLine("                 WIRED IS NOT INSTALLED", ConsoleColor.Red);
            Console.WriteLine("    Add 3583223837 to your WorkshopDownloadConfig.json", ConsoleColor.Red);
            Console.WriteLine("");
            Console.WriteLine(" For more information visit Wired™ page on Steam Workshop", ConsoleColor.Red);
            Console.WriteLine("");
            Console.WriteLine("##########################################################", ConsoleColor.Red);
            Console.WriteLine("##########################################################", ConsoleColor.Red);
        }
    }
}
