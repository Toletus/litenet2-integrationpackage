using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Toletus.Pack.Core.Network.Utils;

namespace Toletus.LiteNet2.Base.Utils;

public abstract class LiteNetUtil
{
    /// <summary>Janela padrão de coleta da varredura. Calibrável em bancada (T-061).</summary>
    public static TimeSpan DefaultScanWindow { get; set; } = TimeSpan.FromSeconds(5);

    // Fan-out de respostas UDP para todas as sessões ativas — cada sessão coleta
    // de forma independente (AC-003.3). Sem estado estático compartilhado de resultado.
    private static readonly object _sessionsLock = new();
    private static readonly List<LiteNetScanSession> _activeSessions = new();

    static LiteNetUtil()
    {
        UdpUtils.OnUdpResponse += OnUdpResponse;
    }

    public static List<LiteNet2BoardBase>? Search(IPAddress networkIpAddress,
        IEnumerable<IPAddress>? exclusions = null, TimeSpan? window = null)
    {
        var session = new LiteNetScanSession(window ?? DefaultScanWindow, exclusions);

        lock (_sessionsLock) _activeSessions.Add(session);
        try
        {
            UdpUtils.Send(networkIpAddress, 7878, "prc");
            session.WaitWindow();
        }
        finally
        {
            lock (_sessionsLock) _activeSessions.Remove(session);
        }

        return session.Resolve(networkIpAddress);
    }

    public static LiteNet2BoardBase? Search(string networkInterfaceName, string serialNumber)
    {
        var liteNets = Search(networkInterfaceName);

        return liteNets?.FirstOrDefault(c => c.SerialNumber == serialNumber);
    }

    public static List<LiteNet2BoardBase>? Search(string networkInterfaceName,
        IEnumerable<IPAddress>? exclusions = null)
    {
        var ip = NetworkInterfaceUtils.GetNetworkInterfaceIpAddressByName(networkInterfaceName);

        return ip == null ? null : Search(ip, exclusions);
    }

    private static void OnUdpResponse(UdpClient udpClient, Task<UdpReceiveResult> response)
    {
        LiteNetScanSession[] sessions;
        lock (_sessionsLock) sessions = _activeSessions.ToArray();

        if (sessions.Length == 0)
            return;

        byte[] buffer;
        IPAddress address;
        try
        {
            buffer = response.Result.Buffer;
            address = response.Result.RemoteEndPoint.Address;
        }
        catch (Exception e)
        {
            LiteNet2BoardBase.Log?.Invoke($"Scan: UDP receive error discarded: {e.Message}");
            return;
        }

        foreach (var session in sessions)
            session.Offer(buffer, address);
    }
}
