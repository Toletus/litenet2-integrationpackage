using System;
using System.Threading;

namespace Toletus.LiteNet2.Base;

public sealed class HealthCheck : IDisposable
{
    private readonly LiteNet2BoardBase _board;
    private Timer? _jobTimer;
    private int _checkInProgress;
    private bool _disposed;

    public HealthCheck(LiteNet2BoardBase board)
    {
        _board = board ?? throw new ArgumentNullException(nameof(board));
        Start();
    }

    private void Start()
    {
        _jobTimer ??= new Timer(ExecuteCheck, state: null,
            dueTime: TimeSpan.FromSeconds(30),
            period: TimeSpan.FromSeconds(10));
    }

    private void Stop()
    {
        _jobTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _jobTimer?.Dispose();
        _jobTimer = null;
    }

    private void ExecuteCheck(object? state)
    {
        if (_disposed) return;
        if (Interlocked.Exchange(ref _checkInProgress, 1) == 1) return;

        try
        {
            Check();
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            Interlocked.Exchange(ref _checkInProgress, 0);
        }
    }

    private void Check()
    {
        if (_disposed) return;
        if (!_board.Connected)
        {
            _board.TryReconnect();
            return;
        }

        try
        {
            _board.CheckConnection();
        }
        catch (Exception)
        {
            _board.Close();
            _board.TryReconnect();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Stop();
    }
}
