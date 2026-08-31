using System.Collections.Generic;

namespace Toletus.LiteNet2.Base;

/// <summary>
/// Remonta frames de tamanho fixo a partir de leituras arbitrárias do socket,
/// preservando o resto entre leituras (AC-006.1). Corrige o fatiamento em blocos
/// de 20 que descartava bytes quando uma mensagem cruzava a fronteira do read.
/// Não é thread-safe: usado por um único laço de recepção por conexão.
/// </summary>
public sealed class FrameAssembler
{
    private readonly int _frameSize;
    private byte[] _carry;
    private int _carryLength;

    public FrameAssembler(int frameSize)
    {
        _frameSize = frameSize;
        _carry = new byte[frameSize];
        _carryLength = 0;
    }

    /// <summary>Alimenta os <paramref name="count"/> bytes lidos e devolve os frames completos.</summary>
    public IEnumerable<byte[]> Push(byte[] data, int count)
    {
        var offset = 0;

        while (offset < count)
        {
            var needed = _frameSize - _carryLength;
            var available = count - offset;
            var take = available < needed ? available : needed;

            System.Array.Copy(data, offset, _carry, _carryLength, take);
            _carryLength += take;
            offset += take;

            if (_carryLength == _frameSize)
            {
                var frame = new byte[_frameSize];
                System.Array.Copy(_carry, frame, _frameSize);
                _carryLength = 0;
                yield return frame;
            }
        }
    }
}
