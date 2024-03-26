using System;
using Humanizer;
using Toletus.LiteNet2.Command.Enums;

namespace FullConsoleAppExample.LiteNet;

internal class ReleaseMenu
{
    public static void Menu()
    {
        while (true)
        {

            Console.WriteLine("");
            Console.WriteLine($"{nameof(ReleaseMenu).Humanize(LetterCasing.Title)}:");
            Console.WriteLine($"     0 - {LiteNet2Commands.ReleaseEntry.Humanize()}");
            Console.WriteLine($"     1 - {LiteNet2Commands.ReleaseExit.Humanize()}");
            Console.WriteLine($" other - Return");

            var liberar = Console.ReadLine();

            switch (liberar)
            {
                case "0":
                    MainMenu.LiteNet2.Send(LiteNet2Commands.ReleaseEntry);
                    break;
                case "1":
                    MainMenu.LiteNet2.Send(LiteNet2Commands.ReleaseExit);
                    break;
                default:
                    return;
            }
        }
    }
}