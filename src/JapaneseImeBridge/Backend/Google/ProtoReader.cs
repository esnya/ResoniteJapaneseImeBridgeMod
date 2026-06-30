using System.Text;

namespace JapaneseImeBridge.Backend.Google;

internal ref struct ProtoReader(ReadOnlySpan<byte> data)
{
    private ReadOnlySpan<byte> remaining = data;

    public bool TryReadField(out int fieldNumber, out int wireType)
    {
        fieldNumber = 0;
        wireType = 0;
        if (remaining.IsEmpty)
        {
            return false;
        }

        var tag = ReadVarint();
        fieldNumber = (int)(tag >> 3);
        wireType = (int)(tag & 0x07);
        return true;
    }

    public ulong ReadVarint()
    {
        ulong value = 0;
        var shift = 0;
        while (!remaining.IsEmpty)
        {
            var b = remaining[0];
            remaining = remaining[1..];
            value |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return value;
            }

            shift += 7;
        }

        throw new InvalidDataException("Unexpected end of protobuf varint.");
    }

    public ReadOnlySpan<byte> ReadLengthDelimited()
    {
        var length = checked((int)ReadVarint());
        if (length > remaining.Length)
        {
            throw new InvalidDataException("Unexpected end of protobuf message.");
        }

        var value = remaining[..length];
        remaining = remaining[length..];
        return value;
    }

    public string ReadString() => Encoding.UTF8.GetString(ReadLengthDelimited());

    public void SkipField(int wireType)
    {
        switch (wireType)
        {
            case 0:
                _ = ReadVarint();
                break;
            case 1:
                SkipBytes(sizeof(ulong));
                break;
            case 2:
                _ = ReadLengthDelimited();
                break;
            case 5:
                SkipBytes(sizeof(uint));
                break;
            default:
                throw new InvalidDataException($"Unsupported protobuf wire type: {wireType}.");
        }
    }

    public void SkipGroup(int expectedFieldNumber)
    {
        while (TryReadField(out var fieldNumber, out var wireType))
        {
            if (wireType == 4 && fieldNumber == expectedFieldNumber)
            {
                return;
            }

            if (wireType == 3)
            {
                SkipGroup(fieldNumber);
            }
            else
            {
                SkipField(wireType);
            }
        }
    }

    private void SkipBytes(int length)
    {
        if (length > remaining.Length)
        {
            throw new InvalidDataException("Unexpected end of protobuf fixed field.");
        }

        remaining = remaining[length..];
    }
}
