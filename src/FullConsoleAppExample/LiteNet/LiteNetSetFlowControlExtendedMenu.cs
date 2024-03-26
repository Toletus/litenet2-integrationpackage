using System;
using Humanizer;
using Toletus.LiteNet2.Command.Enums;

namespace FullConsoleAppExample.LiteNet;

internal class LiteNetSetFlowControlExtendedMenu
{
    public static void Menu()
    {
        while (true)
        {
            Console.WriteLine("");
            Console.WriteLine($"{LiteNet2Commands.SetFlowControlExtended}:");

            var enumLength = Enum.GetNames(typeof(ControlledFlowExtended)).Length;
                
            for (var i = 0; i < enumLength; i++)
                Console.WriteLine($"     {i} - {((ControlledFlowExtended)i).Humanize()}");

            Console.WriteLine($" other - Return");

            var option = Console.ReadLine();

            if (string.IsNullOrEmpty(option) || !int.TryParse(option, out var flow)) return;

            if (flow >= enumLength) return;

            MainMenu.LiteNet2.Send(LiteNet2Commands.SetFlowControlExtended, flow);
        }
    }
}