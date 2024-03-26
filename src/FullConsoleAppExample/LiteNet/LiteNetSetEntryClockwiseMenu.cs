using System;
using Humanizer;
using Toletus.LiteNet2.Command.Enums;

namespace FullConsoleAppExample.LiteNet;

internal class LiteNetSetEntryClockwiseMenu
{
    public static void Menu()
    {
        while (true)
        {
            Console.WriteLine("");
            Console.WriteLine($"{LiteNet2Commands.SetEntryClockwise.Humanize()}:");
            Console.WriteLine($"     0 - True");
            Console.WriteLine($"     1 - False");
            Console.WriteLine($" other - Return");

            var entradaGiroSentidoHorario = Console.ReadLine();

            switch (entradaGiroSentidoHorario)
            {
                case "0":
                    MainMenu.LiteNet2.Send(LiteNet2Commands.GetEntryClockwise, BitConverter.GetBytes(0x01));
                    break;
                case "1":
                    MainMenu.LiteNet2.Send(LiteNet2Commands.GetEntryClockwise, BitConverter.GetBytes(0x00));
                    break;
                default:
                    return;
            }
        }
    }
}