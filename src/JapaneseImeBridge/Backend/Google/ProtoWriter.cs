using System.Text;

namespace JapaneseImeBridge.Backend.Google;

internal sealed class ProtoWriter : IDisposable
{
    private readonly MemoryStream stream = new();

    public byte[] ToArray() => stream.ToArray();

    public void Dispose() => stream.Dispose();

    public void WriteUInt32(int fieldNumber, uint value)
    {
        WriteTag(fieldNumber, 0);
        WriteVarint(value);
    }

    public void WriteUInt64(int fieldNumber, ulong value)
    {
        WriteTag(fieldNumber, 0);
        WriteVarint(value);
    }

    public void WriteBool(int fieldNumber, bool value) => WriteUInt32(fieldNumber, value ? 1u : 0u);

    public void WriteString(int fieldNumber, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(fieldNumber, 2);
        WriteVarint((ulong)bytes.Length);
        stream.Write(bytes);
    }

    public void WriteMessage(int fieldNumber, Action<ProtoWriter> write)
    {
        using var nested = new ProtoWriter();
        write(nested);
        var bytes = nested.ToArray();
        WriteTag(fieldNumber, 2);
        WriteVarint((ulong)bytes.Length);
        stream.Write(bytes);
    }

    private void WriteTag(int fieldNumber, int wireType) => WriteVarint((ulong)((fieldNumber << 3) | wireType));

    private void WriteVarint(ulong value)
    {
        while (value >= 0x80)
        {
            stream.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }

        stream.WriteByte((byte)value);
    }
}
