using System;
using System.Text;
using Humanizer;
using Toletus.LiteNet2.Command.Enums;

namespace FullConsoleAppExample.LiteNet
{
    internal class LiteNetSetMessageMenu
    {
        public static void Menu(LiteNet2Commands command)
        {
            while (true)
            {
                Console.WriteLine("");
                Console.WriteLine($"{command.Humanize(LetterCasing.Title)}:");
                Console.WriteLine($"     Type the new {command.Humanize(LetterCasing.LowerCase)} (16 characters)");
                Console.WriteLine($" ENTER - Return");

                var option = Console.ReadLine();

                if (string.IsNullOrEmpty(option)) return;

                option = Toletus.Pack.Core.Extensions.StringExtensions.Truncate(option, 16);

                MainMenu.LiteNet2.Send(command, Encoding.ASCII.GetBytes(option));

                Console.WriteLine($"The message was setted");
            }
        }
    }
}