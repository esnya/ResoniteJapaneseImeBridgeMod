using JapaneseImeBridge.Backend;
using JapaneseImeBridge.Backend.Google;

namespace JapaneseImeBridge.Tests;

public sealed class BridgeEngineTests
{
    [Fact]
    public void GoogleJapaneseInputRuntimeDetectsConverterInDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"JapaneseImeBridgeTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var converterPath = Path.Combine(directory, "GoogleIMEJaConverter.exe");
            File.WriteAllText(converterPath, string.Empty);

            var runtime = GoogleJapaneseInputRuntime.Detect(directory);

            Assert.NotNull(runtime);
            Assert.Equal(converterPath, runtime.ConverterPath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void GoogleIpcCodecWritesInputMessageAndParsesOutputMessage()
    {
        var createSession = MozcIpcCommandCodec.CreateSession();

        using var output = new ProtoWriter();
        output.WriteUInt64(1, 42);
        output.WriteBool(3, true);
        output.WriteMessage(4, result =>
        {
            result.WriteUInt32(1, 1);
            result.WriteString(2, "か");
        });
        output.WriteMessage(14, candidates =>
        {
            candidates.WriteUInt32(1, 1);
            candidates.WriteMessage(2, candidate => candidate.WriteString(4, "可"));
            candidates.WriteMessage(2, candidate => candidate.WriteString(4, "蚊"));
        });

        var parsed = MozcIpcCommandCodec.ParseOutput(output.ToArray());

        Assert.Equal([0x08, 0x01], createSession);
        Assert.Equal<ulong>(42, parsed.SessionId);
        Assert.True(parsed.Consumed);
        Assert.Equal("か", parsed.CommitText);
        Assert.Equal(["可", "蚊"], parsed.Candidates);
        Assert.Equal(1, parsed.FocusedCandidateIndex);
    }

    [Fact]
    public void GoogleIpcCodecWritesSegmentWidthKeysWithShiftModifier()
    {
        var left = ReadKeyEvent(MozcIpcCommandCodec.SendKey(42, new ImeBackendRequest(1, ImeBackendCommand.Key, Key: ImeBackendKey.Left)));
        var expand = ReadKeyEvent(MozcIpcCommandCodec.SendKey(42, new ImeBackendRequest(2, ImeBackendCommand.Key, Key: ImeBackendKey.SegmentWidthExpand)));
        var shrink = ReadKeyEvent(MozcIpcCommandCodec.SendKey(42, new ImeBackendRequest(3, ImeBackendCommand.Key, Key: ImeBackendKey.SegmentWidthShrink)));

        Assert.Equal<ulong>(6, left.SpecialKey);
        Assert.Empty(left.ModifierKeys);
        Assert.Equal<ulong>(7, expand.SpecialKey);
        Assert.Equal([4UL], expand.ModifierKeys);
        Assert.Equal<ulong>(6, shrink.SpecialKey);
        Assert.Equal([4UL], shrink.ModifierKeys);
    }

    private static KeyEventMessage ReadKeyEvent(byte[] bytes)
    {
        var reader = new ProtoReader(bytes);
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 3 && wireType == 2)
            {
                return ReadNestedKeyEvent(reader.ReadLengthDelimited());
            }

            reader.SkipField(wireType);
        }

        throw new InvalidDataException("KeyEvent field was not found.");
    }

    private static KeyEventMessage ReadNestedKeyEvent(ReadOnlySpan<byte> bytes)
    {
        ulong specialKey = 0;
        var modifierKeys = new List<ulong>();
        var reader = new ProtoReader(bytes);
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 3 && wireType == 0)
            {
                specialKey = reader.ReadVarint();
            }
            else if (fieldNumber == 4 && wireType == 0)
            {
                modifierKeys.Add(reader.ReadVarint());
            }
            else
            {
                reader.SkipField(wireType);
            }
        }

        return new KeyEventMessage(specialKey, modifierKeys);
    }

    private sealed record KeyEventMessage(ulong SpecialKey, IReadOnlyList<ulong> ModifierKeys);
}
