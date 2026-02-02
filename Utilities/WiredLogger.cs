using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wired.Utilities
{
    public class WiredLogger
    {
        public static void Log(string message)
        {
            Console.WriteLine($"[Wired]: {message}");
        }
        public static void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Wired] WARN: {message}");
            Console.ResetColor();
        }
        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Wired] ERROR: {message}");
            Console.ResetColor();
        }

        public static void PluginLoaded(bool success)
        {
            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("           _              _ ");
                Console.WriteLine("          (_)            | |");
                Console.WriteLine(" __      ___ _ __ ___  __| |");
                Console.WriteLine(" \\ \\ /\\ / / | '__/ _ \\/ _` |        Wired has loaded succesfully!");
                Console.WriteLine("  \\ V  V /| | | |  __/ (_| |");
                Console.WriteLine("   \\_/\\_/ |_|_|  \\___|\\__,_|\n");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("##########################################################");
            Console.WriteLine("##########################################################");
            Console.WriteLine("");
            Console.WriteLine("                 WIRED IS NOT INSTALLED");
            Console.WriteLine("    Add 3583223837 to your WorkshopDownloadConfig.json");
            Console.WriteLine("");
            Console.WriteLine("##########################################################");
            Console.WriteLine("##########################################################");
            Console.ResetColor();
        }
    }
}
