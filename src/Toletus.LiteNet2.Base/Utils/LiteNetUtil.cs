using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Toletus.Pack.Core.Network.Utils;
using Toletus.Pack.Core.Utils;

namespace Toletus.LiteNet2.Base.Utils;

public abstract class LiteNetUtil
{
    const string ToletusLiteNet2 = "TOLETUS LiteNet2";
    private static List<LiteNet2BoardBase> _liteNets = new();

    static LiteNetUtil()
    {
        UdpUtils.OnUdpResponse += OnUdpResponse;
    }

    public static List<LiteNet2BoardBase>? Search(IPAddress networkIpAddress)
    {
        _liteNets.Clear();

        UdpUtils.Send(networkIpAddress, 7878, "prc");

        foreach (var liteNet in _liteNets)
            liteNet.NetworkIp = networkIpAddress;

        return _liteNets;
    }

    public static LiteNet2BoardBase? Search(string networkInterfaceName, int? id)
    {
        var liteNets = Search(networkInterfaceName);

        return liteNets?.FirstOrDefault(c => id == null || c.Id == id);
    }

    public static List<LiteNet2BoardBase>? Search(string networkInterfaceName)
    {
        var ip = NetworkInterfaceUtils.GetNetworkInterfaceIpAddressByName(networkInterfaceName);

        return ip == null ? null : Search(ip);
    }

    private static void OnUdpResponse(UdpClient udpClient, Task<UdpReceiveResult> response)
    {
        var device = Encoding.ASCII.GetString(response.Result.Buffer);

        if (!device.Contains(ToletusLiteNet2))
            return;

        var m = Regex.Match(device, @"@(\d+)");
        var id = (UInt16)Convert.ToInt16(m.Groups[1].Value);
        
        var connectionInfo = string.Empty;

        var start = device.IndexOf('=');
        
        if (!(start < 0))
            connectionInfo = device.Substring(start + 1).Trim();

        var liteNet = new LiteNet2BoardBase(response.Result.RemoteEndPoint.Address, id, connectionInfo);

        _liteNets.Add(liteNet);
    }
}