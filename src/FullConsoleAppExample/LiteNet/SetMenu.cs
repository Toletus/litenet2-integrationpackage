using System;
using Humanizer;
using Toletus.LiteNet2.Command.Enums;

namespace FullConsoleAppExample.LiteNet;

internal class SetMenu
{
    public static void Menu()
    {
        while (true)
        {
            Console.WriteLine("");
            Console.WriteLine($"{nameof(SetMenu).Humanize(LetterCasing.Title)}:");
            Console.WriteLine($"     0 - {LiteNet2Commands.SetFlowControl.Humanize()}");
            Console.WriteLine($"     1 - {LiteNet2Commands.SetFlowControlExtended.Humanize()}");
            Console.WriteLine($"     2 - {LiteNet2Commands.SetEntryClockwise.Humanize()}");
            Console.WriteLine($"     3 - {LiteNet2Commands.SetMenuPassword.Humanize()}");
            Console.WriteLine($"     4 - {LiteNet2Commands.SetMessageLine1.Humanize()}");
            Console.WriteLine($"     5 - {LiteNet2Commands.SetMessageLine2.Humanize()}");
            Console.WriteLine($"     6 - {LiteNet2Commands.SetBuzzerMute.Humanize()}");
            Console.WriteLine($"     7 - {LiteNet2Commands.SetShowCounters.Humanize()}");
            Console.WriteLine($"     8 - {LiteNet2Commands.ResetCounters.Humanize()}");
            Console.WriteLine($" other - Return");

            var option = Console.ReadLine();

            switch (option)
            {
                case "0":
                    LiteNetSetFlowControlMenu.Menu();
                    break;
                case "1":
                    LiteNetSetFlowControlExtendedMenu.Menu();
                    break;
                case "2":
                    LiteNetSetEntryClockwiseMenu.Menu();
                    break;
                case "3":
                    LiteNetSetPasswordMenu.Menu();
                    break;
                case "4":
                    LiteNetSetMessageMenu.Menu(LiteNet2Commands.SetMessageLine1);
                    break;
                case "5":
                    LiteNetSetMessageMenu.Menu(LiteNet2Commands.SetMessageLine2);
                    break;
                case "6":
                    LiteNetSetBoolMenu.Menu(LiteNet2Commands.SetBuzzerMute);
                    break;
                case "7":
                    LiteNetSetBoolMenu.Menu(LiteNet2Commands.SetShowCounters);
                    break;
                case "8":
                    MainMenu.LiteNet2.Send(LiteNet2Commands.ResetCounters);
                    break;
                default:
                    return;
            }
        }
    }
}