using System;
using Toletus.LiteNet2.Command.Enums;

namespace FullConsoleAppExample.LiteNet
{
    internal class LiteNetSetFlowControlMenu
    {
        public static void Menu()
        {
            while (true)
            {
                Console.WriteLine("");
                Console.WriteLine($"{LiteNet2Commands.SetFlowControl}:");
                Console.WriteLine($"     0 - {ControlledFlow.None}");
                Console.WriteLine($"     1 - {ControlledFlow.Entry}");
                Console.WriteLine($"     2 - {ControlledFlow.Exit}");
                Console.WriteLine($"     3 - {ControlledFlow.Both}");
                Console.WriteLine($" other - Return");

                var option = Console.ReadLine();

                if (string.IsNullOrEmpty(option) || "0123".IndexOf(option, StringComparison.Ordinal) < 0) return;

                var flow = (ControlledFlow)int.Parse(option);

                MainMenu.LiteNet2.Send(LiteNet2Commands.SetFlowControl, (int)flow);
            }
        }
    }
}