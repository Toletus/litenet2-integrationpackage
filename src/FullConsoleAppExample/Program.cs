using System;
using ConsoleTools;
using FullConsoleAppExample.LiteNet;
using Toletus.LiteNet2.Base;

namespace FullConsoleAppExample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            while (true)
            {
                var menu = new ConsoleMenu(args, level: 0)
                    .Add("Search for Toletus LiteNets2", () => LiteNetSearchMenu.Menu())
                    .Add("Exit", () => Environment.Exit(0));
                menu.Show();
            }
        }

        public static LiteNet2BoardBase LiteNet2 { get; set; }
    }
}