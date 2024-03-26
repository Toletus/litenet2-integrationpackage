using System.Linq;
using System.Net;
using ConsoleTools;
using Toletus.LiteNet2.Base;
using Toletus.LiteNet2.Base.Utils;
using Toletus.Pack.Core.Utils;

namespace FullConsoleAppExample.LiteNet;

internal class LiteNetSearchMenu
{
    public static void Menu()
    {
        var menu = new ConsoleMenu();
        var networks = NetworkInterfaceUtils.GetNetworkInterfaces();

        for (int i = 0; i < networks.Count; i++)
            menu.Add(networks.ElementAt(i).Key, (c) => SearchForLiteNets(networks.ElementAt(c.CurrentItem.Index).Value));

        menu.Add("Return", (c) => c.CloseMenu());
            
        menu.Show();
    }

    private static void SearchForLiteNets(IPAddress value)
    {
        var liteNets = LiteNetUtil.Search(value);

        var menu = new ConsoleMenu();

        foreach (var liteNet in liteNets) 
            menu.Add(liteNet.ToString(), () =>
            {
                SelectLiteNet(liteNet);
                menu.CloseMenu();
            });

        menu.Add("Return", (c) => c.CloseMenu());
        menu.Show();
    }

    private static void SelectLiteNet(LiteNet2BoardBase liteNet)
    {
        MainMenu.SetLiteNet2(liteNet);
    }
}