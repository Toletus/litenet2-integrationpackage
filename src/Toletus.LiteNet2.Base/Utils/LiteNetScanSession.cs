using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Toletus.LiteNet2.Base.Utils;

/// <summary>
/// Sessão de varredura isolada (TDD §4.4): estado próprio (fim do estado estático),
/// coleta pela janela inteira (AC-003.1), exclusões de endereços já conectados sem nova
/// conexão (AC-002.3), consulta de identidade com teto curto (AC-003.4) e parse protegido
/// contra resposta malformada (AC-003.2). Duas sessões concorrentes não se corrompem (AC-003.3).
/// </summary>
public sealed class LiteNetScanSession
{
    private const string ToletusLiteNet2 = "TOLETUS LiteNet2";

    private readonly TimeSpan _window;
    private readonly HashSet<string> _exclusions;
    private readonly int _resolvePort;
    private readonly object _lock = new();
    private readonly Dictionary<IPAddress, LiteNet2BoardBase> _collected = new();

    public LiteNetScanSession(TimeSpan window, IEnumerable<IPAddress>? exclusions = null,
        int resolvePort = LiteNet2BoardBase.Port)
    {
        _window = window;
        _resolvePort = resolvePort;
        _exclusions = new HashSet<string>(
            (exclusions ?? Enumerable.Empty<IPAddress>()).Select(a => a.ToString()));
    }

    /// <summary>Chamado pelo fan-out de respostas UDP; só coleta, nunca abre conexão.</summary>
    public void Offer(byte[] payload, IPAddress remoteAddress)
    {
        try
        {
            var device = Encoding.ASCII.GetString(payload);
            if (!device.Contains(ToletusLiteNet2))
                return;

            var m = Regex.Match(device, @"@(\d+)");
            if (!m.Success)
                return;

            var id = (ushort)Convert.ToInt16(m.Groups[1].Value);

            var connectionInfo = string.Empty;
            var start = device.IndexOf('=');
            if (start >= 0)
                connectionInfo = device.Substring(start + 1).Trim();

            var board = new LiteNet2BoardBase(ip: remoteAddress, id: id, connectionInfo: connectionInfo)
            {
                ConnectPort = _resolvePort
            };

            lock (_lock)
                _collected[remoteAddress] = board; // dedup por endereço
        }
        catch (Exception e)
        {
            // Resposta malformada: descarta sem interromper a varredura (AC-003.2).
            LiteNet2BoardBase.Log?.Invoke($"Scan: malformed UDP response discarded: {e.Message}");
        }
    }

    /// <summary>Bloqueia pela janela inteira — não encerra na 1ª resposta (AC-003.1).</summary>
    public void WaitWindow() => Thread.Sleep(_window);

    /// <summary>
    /// Resolve o serial dos coletados. Endereços excluídos (já conectados) NÃO recebem
    /// conexão (AC-002.3) — entram no resultado sem serial, a identidade vem do Registro.
    /// Cada consulta tem teto próprio (AC-003.4).
    /// </summary>
    public List<LiteNet2BoardBase> Resolve(IPAddress networkIpAddress)
    {
        LiteNet2BoardBase[] boards;
        lock (_lock)
            boards = _collected.Values.ToArray();

        var toResolve = boards.Where(b => !_exclusions.Contains(b.Ip.ToString())).ToArray();

        Task.WhenAll(toResolve.Select(async b =>
        {
            b.NetworkIp = networkIpAddress;
            await b.FetchAndSetSerialNumberAsync().ConfigureAwait(false); // teto interno = ConnectTimeout
        })).GetAwaiter().GetResult();

        foreach (var b in boards)
            b.NetworkIp = networkIpAddress;

        return boards.ToList();
    }
}
