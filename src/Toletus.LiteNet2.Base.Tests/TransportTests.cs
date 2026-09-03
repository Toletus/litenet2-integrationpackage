using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;
using Xunit;

namespace Toletus.LiteNet2.Base.Tests;

public class TransportTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static byte[] ValidFrame() => new LiteNet2Send(LiteNet2Commands.GetId).Payload;

    private static byte[] MalformedIdentificationFrame()
    {
        var data = new byte[16];
        var letters = System.Text.Encoding.ASCII.GetBytes("NOTANUMBER"); // int.Parse vai lançar
        Array.Copy(letters, data, letters.Length);
        return new LiteNet2Send(LiteNet2Commands.IdentificationByKeyboard, data).Payload;
    }

    private static LiteNet2BoardBase Connect(FakeBoardServer server)
    {
        var board = new LiteNet2BoardBase(IPAddress.Loopback) { ConnectPort = server.Port };
        board.Connect();
        Assert.True(server.WaitForConnection(Timeout));
        return board;
    }

    private static bool WaitUntil(Func<bool> condition, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (condition()) return true;
            Thread.Sleep(10);
        }
        return condition();
    }

    [Fact]
    public void Connect_raises_Connected_event()
    {
        using var server = new FakeBoardServer();
        ConnectionStateChange? last = null;
        var board = new LiteNet2BoardBase(IPAddress.Loopback) { ConnectPort = server.Port };
        board.OnConnectionStatusChanged += (_, c) => last = c;

        board.Connect();

        Assert.True(board.Connected);
        Assert.NotNull(last);
        Assert.Equal(BoardConnectionStatus.Connected, last!.Status);
        Assert.Equal(ConnectionCause.None, last.Cause);
    }

    // AC-005.1 — envio a desconectado devolve erro, nunca descarte silencioso.
    [Fact]
    public void Send_to_never_connected_returns_Disconnected_error()
    {
        var board = new LiteNet2BoardBase(IPAddress.Loopback) { ConnectPort = 65000 };

        var result = board.Send(LiteNet2Commands.GetId);

        Assert.False(result.Success);
        Assert.Equal(SendResult.SendError.Disconnected, result.Error);
    }

    // AC-004.3 — encerramento remoto ordenado publica estado com causa.
    [Fact]
    public void Graceful_remote_close_raises_Failed_with_RemoteClose()
    {
        using var server = new FakeBoardServer();
        var causes = new ConcurrentQueue<ConnectionStateChange>();
        var board = Connect(server);
        board.OnConnectionStatusChanged += (_, c) => causes.Enqueue(c);

        server.CloseGraceful();

        Assert.True(WaitUntil(() => !causes.IsEmpty, Timeout)); // espera o evento chegar, não só o socket fechar
        Assert.False(board.Connected);
        Assert.Contains(causes, c => c.Status == BoardConnectionStatus.Failed
                                     && c.Cause == ConnectionCause.RemoteClose);
    }

    // AC-004.1/004.3 — queda abrupta (reset) detectada com causa.
    [Fact]
    public void Reset_raises_Failed_with_ReceiveError()
    {
        using var server = new FakeBoardServer();
        var causes = new ConcurrentQueue<ConnectionStateChange>();
        var board = Connect(server);
        board.OnConnectionStatusChanged += (_, c) => causes.Enqueue(c);

        server.CloseWithReset();

        Assert.True(WaitUntil(() => !causes.IsEmpty, Timeout));
        Assert.False(board.Connected);
        Assert.Contains(causes, c => c.Status == BoardConnectionStatus.Failed
                                     && c.Cause == ConnectionCause.ReceiveError);
    }

    // AC-005.1 — depois da queda, envio devolve falha (nunca "sucesso" silencioso).
    [Fact]
    public void Send_after_drop_returns_failure()
    {
        using var server = new FakeBoardServer();
        var board = Connect(server);
        server.CloseWithReset();
        WaitUntil(() => !board.Connected, Timeout);

        var result = board.Send(LiteNet2Commands.GetId);

        Assert.False(result.Success);
    }

    // AC-006.1 (e2e) — resposta fragmentada em fronteiras arbitrárias remontada e entregue.
    [Fact]
    public void Fragmented_response_is_reassembled_and_delivered()
    {
        using var server = new FakeBoardServer();
        var responses = new ConcurrentQueue<LiteNet2Response>();
        var board = Connect(server);
        board.OnResponse += r => responses.Enqueue(r);

        server.PushBytes(ValidFrame(), chunkSize: 7); // 20 bytes em pedaços de 7

        Assert.True(WaitUntil(() => responses.Count == 1, Timeout));
        Assert.Equal(LiteNet2Commands.GetId, responses.TryPeek(out var r) ? r.Command : default);
    }

    // AC-006.2/006.3 — mensagem malformada é descartada e a conexão segue viva.
    [Fact]
    public void Malformed_message_is_discarded_connection_survives()
    {
        using var server = new FakeBoardServer();
        var responses = new ConcurrentQueue<LiteNet2Response>();
        var board = Connect(server);
        board.OnResponse += r => responses.Enqueue(r);

        server.PushBytes(MalformedIdentificationFrame(), chunkSize: 20, delayMsBetweenChunks: 0);
        Thread.Sleep(200);
        server.PushBytes(ValidFrame(), chunkSize: 20, delayMsBetweenChunks: 0);

        // A malformada não derrubou a conexão e a válida seguinte chega.
        Assert.True(WaitUntil(() => responses.Count == 1, Timeout));
        Assert.True(board.Connected);
    }

    // AC-006.4 — handler lento não bloqueia a recepção (fila de despacho decopla).
    [Fact]
    public void Slow_handler_does_not_block_receive()
    {
        using var server = new FakeBoardServer();
        var delivered = 0;
        var board = Connect(server);
        board.OnResponse += _ =>
        {
            Interlocked.Increment(ref delivered);
            Thread.Sleep(150); // handler lento
        };

        const int n = 5;
        for (var i = 0; i < n; i++)
            server.PushBytes(ValidFrame(), chunkSize: 20, delayMsBetweenChunks: 0);

        // Todos os frames foram recebidos e enfileirados sem perda apesar do handler lento.
        Assert.True(WaitUntil(() => delivered == n, TimeSpan.FromSeconds(10)));
        Assert.True(board.Connected);
    }

    // ADR-001 REVERTIDO: o transporte VOLTA a se recuperar sozinho via HealthCheck
    // (poll GetId 10s + TryReconnect) — mecanismo comprovado em produção. O HealthCheck
    // só age a partir de dueTime=30s, então logo após a queda ainda não há reconexão.
    [Fact]
    public void Drop_raises_failure_and_recovery_is_owned_by_healthcheck()
    {
        using var server = new FakeBoardServer();
        var board = Connect(server);
        Assert.Equal(1, server.ConnectionCount);

        server.CloseWithReset();
        WaitUntil(() => !board.Connected, Timeout);

        Assert.False(board.Connected);
        Assert.Equal(1, server.ConnectionCount); // HealthCheck ainda não entrou (dueTime 30s)
    }
}
