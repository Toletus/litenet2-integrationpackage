using System;
using ConsoleTools;
using FullConsoleAppExample.FingerprintReader;
using FullConsoleAppExample.LiteNet;
using Humanizer;
using Toletus.LiteNet2.Base;
using Toletus.LiteNet2.Command;

namespace FullConsoleAppExample;

internal static class MainMenu
{
    internal static LiteNet2BoardBase LiteNet2;

    public static void SetLiteNet2(LiteNet2BoardBase liteNet)
    {
        LiteNet2 = liteNet;
        LiteNet2.OnResponse += LiteNet2_OnResponse;

        //LiteNet2.Connect();

        Menu();
    }

    public static void Menu()
    {
        if (LiteNet2.InUse)
        {
            Console.WriteLine("LiteNet2 already in use...");
            return;
        }

        Console.WriteLine("Connecting to LiteNet2...");

        LiteNet2.Connect();

        Console.WriteLine($"Connected! Serial number: {LiteNet2.SerialNumber}");

        var exit = false;
        while (!exit)
        {
            var menu = new ConsoleMenu(new string[] { }, level: 0)
                .Configure(config =>
                {
                    config.Selector = "--> ";
                    config.Title = $"{nameof(MainMenu).Humanize(LetterCasing.Title)}:";
                })
                .Add($"{nameof(SetMenu).Humanize(LetterCasing.Title)}", () => SetMenu.Menu())
                .Add($"{nameof(GetMenu).Humanize(LetterCasing.Title)}", () => GetMenu.Menu())
                .Add($"{nameof(ReleaseMenu).Humanize(LetterCasing.Title)}", () => ReleaseMenu.Menu())
                .Add($"{nameof(FingerprintReaderMenu).Humanize(LetterCasing.Title)}",
                    () => FingerprintReaderMenu.Menu(LiteNet2.Ip))
                .Add($"Return", (c) =>
                {
                    LiteNet2.Close();
                    c.CloseMenu();
                });

            menu.Show();
        }
    }

    private static void LiteNet2_OnResponse(LiteNet2Response responseCommand)
    {
        Console.WriteLine($"Serial number:{LiteNet2.SerialNumber} - {Environment.NewLine}LiteNet2 Response: {responseCommand}]");

        if (responseCommand.Identification != null)
            Console.WriteLine(
                $"{Environment.NewLine}LiteNet2 Identification: {responseCommand.Identification} {responseCommand.Identification.EmbededTemplate}");
    }
}