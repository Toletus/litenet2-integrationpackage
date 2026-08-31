using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Toletus.LiteNet2.Base.Tests;

public class FrameAssemblerTests
{
    private static byte[] Frame(byte marker)
    {
        var f = new byte[20];
        for (var i = 0; i < 20; i++) f[i] = marker;
        return f;
    }

    // AC-006.1 — dados fragmentados remontados sem perda.
    [Fact]
    public void Reassembles_frames_split_across_arbitrary_boundaries()
    {
        var a = new FrameAssembler(20);
        var f1 = Frame(1);
        var f2 = Frame(2);
        var stream = f1.Concat(f2).ToArray(); // 40 bytes

        var frames = new List<byte[]>();
        // fronteira arbitrária: primeiro read entrega 25 bytes (frame1 + 5 do frame2)...
        frames.AddRange(a.Push(stream.Take(25).ToArray(), 25));
        // ...segundo read entrega os 15 restantes.
        frames.AddRange(a.Push(stream.Skip(25).ToArray(), 15));

        Assert.Equal(2, frames.Count);
        Assert.Equal(f1, frames[0]);
        Assert.Equal(f2, frames[1]);
    }

    [Fact]
    public void Reassembles_byte_by_byte()
    {
        var a = new FrameAssembler(20);
        var f1 = Frame(7);
        var frames = new List<byte[]>();

        foreach (var b in f1)
            frames.AddRange(a.Push(new[] { b }, 1));

        Assert.Single(frames);
        Assert.Equal(f1, frames[0]);
    }

    [Fact]
    public void Emits_multiple_frames_from_one_big_read()
    {
        var a = new FrameAssembler(20);
        var stream = Frame(1).Concat(Frame(2)).Concat(Frame(3)).ToArray();

        var frames = a.Push(stream, stream.Length).ToList();

        Assert.Equal(3, frames.Count);
    }

    [Fact]
    public void Holds_partial_frame_until_completed()
    {
        var a = new FrameAssembler(20);

        Assert.Empty(a.Push(new byte[19], 19)); // 19 bytes: nenhum frame completo ainda
        Assert.Single(a.Push(new byte[1], 1));  // o 20º byte completa
    }
}
