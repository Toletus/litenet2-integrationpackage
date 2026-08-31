using System;
using System.Linq;
using System.Net;
using System.Text;
using Toletus.LiteNet2.Base.Utils;
using Xunit;

namespace Toletus.LiteNet2.Base.Tests;

public class ScanSessionTests
{
    private static byte[] Response(int id, string connInfo = "Disconnected")
        => Encoding.ASCII.GetBytes($"TOLETUS LiteNet2 @{id} status={connInfo}");

    private static LiteNetScanSession NewSession(params IPAddress[] exclusions)
        => new(TimeSpan.Zero, exclusions);

    // AC-003.1 — todas as respostas da janela entram no resultado.
    [Fact]
    public void Collects_all_responders()
    {
        var s = NewSession(IPAddress.Parse("127.0.0.2"), IPAddress.Parse("127.0.0.3"),
            IPAddress.Parse("127.0.0.4"));
        s.Offer(Response(1), IPAddress.Parse("127.0.0.2"));
        s.Offer(Response(2), IPAddress.Parse("127.0.0.3"));
        s.Offer(Response(3), IPAddress.Parse("127.0.0.4"));

        var result = s.Resolve(IPAddress.Loopback);

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 1, 2, 3 }, result.Select(b => b.Id).OrderBy(x => x));
    }

    // AC-003.2 — resposta malformada descartada sem interromper a varredura.
    [Fact]
    public void Malformed_response_is_discarded_scan_continues()
    {
        var addr = IPAddress.Parse("127.0.0.2");
        var s = NewSession(addr);

        // marcador presente, mas id estoura Int16 -> Convert lança -> descartada.
        s.Offer(Encoding.ASCII.GetBytes("TOLETUS LiteNet2 @99999999999"), IPAddress.Parse("127.0.0.9"));
        // ruído sem o marcador -> ignorado.
        s.Offer(Encoding.ASCII.GetBytes("garbage"), IPAddress.Parse("127.0.0.8"));
        // válida -> coletada.
        s.Offer(Response(7), addr);

        var result = s.Resolve(IPAddress.Loopback);

        Assert.Single(result);
        Assert.Equal(7, result[0].Id);
    }

    // AC-003.3 — duas varreduras concorrentes produzem resultados independentes.
    [Fact]
    public void Concurrent_sessions_are_independent()
    {
        var a = NewSession(IPAddress.Parse("127.0.1.1"));
        var b = NewSession(IPAddress.Parse("127.0.2.2"));

        a.Offer(Response(10), IPAddress.Parse("127.0.1.1"));
        b.Offer(Response(20), IPAddress.Parse("127.0.2.2"));

        var ra = a.Resolve(IPAddress.Loopback);
        var rb = b.Resolve(IPAddress.Loopback);

        Assert.Single(ra);
        Assert.Equal(10, ra[0].Id);
        Assert.Single(rb);
        Assert.Equal(20, rb[0].Id);
    }

    [Fact]
    public void Deduplicates_by_address()
    {
        var addr = IPAddress.Parse("127.0.0.2");
        var s = NewSession(addr);
        s.Offer(Response(1), addr);
        s.Offer(Response(1), addr); // mesma catraca respondendo de novo

        Assert.Single(s.Resolve(IPAddress.Loopback));
    }

    // AC-002.3 — endereço excluído (já conectado) NÃO recebe nova conexão na varredura.
    [Fact]
    public void Excluded_address_gets_no_connection()
    {
        using var server = new FakeBoardServer();
        var excluded = IPAddress.Loopback;
        var s = new LiteNetScanSession(TimeSpan.Zero, new[] { excluded }, resolvePort: server.Port);

        s.Offer(Response(1, "Connected"), excluded);
        var result = s.Resolve(IPAddress.Loopback);

        Assert.Single(result);                    // segue no resultado (identidade vem do Registro)
        Assert.Equal(0, server.ConnectionCount);  // porém nenhuma conexão foi aberta
    }

    // Controle: endereço NÃO excluído recebe a consulta de identidade (abre conexão).
    [Fact]
    public void Non_excluded_address_is_resolved()
    {
        var original = LiteNet2BoardBase.ConnectTimeout;
        LiteNet2BoardBase.ConnectTimeout = TimeSpan.FromMilliseconds(500); // servidor aceita e cala: teto encerra rápido
        try
        {
            using var server = new FakeBoardServer();
            var s = new LiteNetScanSession(TimeSpan.Zero, exclusions: null, resolvePort: server.Port);

            s.Offer(Response(1), IPAddress.Loopback);
            s.Resolve(IPAddress.Loopback);

            Assert.True(server.ConnectionCount >= 1);
        }
        finally
        {
            LiteNet2BoardBase.ConnectTimeout = original;
        }
    }
}
