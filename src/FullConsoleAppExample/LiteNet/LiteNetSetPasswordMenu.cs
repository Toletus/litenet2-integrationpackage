using System;
using System.Text;
using Humanizer;
using Toletus.LiteNet2.Command.Enums;

namespace FullConsoleAppExample.LiteNet;

internal class LiteNetSetPasswordMenu
{
    public static void Menu()
    {
        while (true)
        {
            Console.WriteLine("");
            Console.WriteLine($"{LiteNet2Commands.SetMenuPassword.Humanize(LetterCasing.Title)}:");
            Console.WriteLine($"     Type the new passowrd (6 digits)");
            Console.WriteLine($" ENTER - Return");

            var option = Console.ReadLine();

            if (string.IsNullOrEmpty(option)) return;

            option = option.Trim();

            if (option.Length != 6)
            {
                Console.WriteLine($"The password must have 6 digits");
                continue;
            }

            var isNumeric = int.TryParse("123", out _);
            if (!isNumeric)
            {
                Console.WriteLine($"The password must have only");
                continue;
            }

            MainMenu.LiteNet2.Send(LiteNet2Commands.SetMenuPassword, Encoding.ASCII.GetBytes(option));

            Console.WriteLine($"The new password was setted");
        }
    }
}