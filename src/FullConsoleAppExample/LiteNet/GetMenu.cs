using System;
using Humanizer;
using Toletus.LiteNet2.Command.Enums;

namespace FullConsoleAppExample.LiteNet
{
    internal class GetMenu
    {
        public static void Menu()
        {
            while (true)
            {
                Console.WriteLine("");
                Console.WriteLine($"{nameof(GetMenu).Humanize(LetterCasing.Title)}:");
                Console.WriteLine($"     0 - {LiteNet2Commands.GetBuzzerMute.Humanize()}");
                Console.WriteLine($"     1 - {LiteNet2Commands.GetFlowControl.Humanize()}");
                Console.WriteLine($"     2 - {LiteNet2Commands.GetFlowControlExtended.Humanize()}");
                Console.WriteLine($"     3 - {LiteNet2Commands.GetEntryClockwise.Humanize()}");
                Console.WriteLine($"     4 - {LiteNet2Commands.GetReleaseDuration.Humanize()}");
                Console.WriteLine($"     5 - {LiteNet2Commands.GetId.Humanize()}");
                Console.WriteLine($"     6 - {LiteNet2Commands.GetMac.Humanize()}");
                Console.WriteLine($"     7 - {LiteNet2Commands.GetFirmwareVersion.Humanize()}");
                Console.WriteLine($"     8 - {LiteNet2Commands.GetMenuPassword.Humanize()}");
                Console.WriteLine($"     9 - {LiteNet2Commands.GetMessageLine1.Humanize()}");
                Console.WriteLine($"    10 - {LiteNet2Commands.GetMessageLine2.Humanize()}");
                Console.WriteLine($"    11 - {LiteNet2Commands.GetShowCounters.Humanize()}");
                Console.WriteLine($"    12 - {LiteNet2Commands.GetBuzzerMute.Humanize()}");
                Console.WriteLine($" other - Return");

                var option = Console.ReadLine();

                switch (option)
                {
                    case "0":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetBuzzerMute);
                        break;
                    case "1":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetFlowControl);
                        break;
                    case "2":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetFlowControlExtended);
                        break;
                    case "3":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetEntryClockwise);
                        break;
                    case "4":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetReleaseDuration);
                        break;
                    case "5":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetId);
                        break;
                    case "6":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetMac);
                        break;
                    case "7":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetFirmwareVersion);
                        break;
                    case "8":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetMenuPassword);
                        break;
                    case "9":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetMessageLine1);
                        break;
                    case "10":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetMessageLine2);
                        break;
                    case "11":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetShowCounters);
                        break;
                    case "12":
                        MainMenu.LiteNet2.Send(LiteNet2Commands.GetBuzzerMute);
                        break;
                    default:
                        return;
                }
            }
        }
    }
}