using JapaneseImeBridge.Backend;

namespace JapaneseImeBridge.Backend.Google;

internal static class MozcIpcCommandCodec
{
    private const uint InputCreateSession = 1;
    private const uint InputSendKey = 3;
    private const uint InputSendCommand = 5;
    private const uint SessionSubmit = 2;
    private const uint SessionRevert = 1;
    private const uint SessionResetContext = 10;
    private const uint SessionTurnOnIme = 22;
    private const uint ModeHiragana = 1;
    private const uint ModifierShift = 4;

    public static byte[] CreateSession() =>
        WriteInput(input => input.WriteUInt32(1, InputCreateSession));

    public static byte[] TurnOnIme(ulong sessionId) =>
        WriteSessionCommand(sessionId, SessionTurnOnIme, ModeHiragana);

    public static byte[] Submit(ulong sessionId) => WriteSessionCommand(sessionId, SessionSubmit);

    public static byte[] Revert(ulong sessionId) => WriteSessionCommand(sessionId, SessionRevert);

    public static byte[] ResetContext(ulong sessionId) => WriteSessionCommand(sessionId, SessionResetContext);

    public static byte[] SendKey(ulong sessionId, ImeBackendRequest request) =>
        WriteInput(input =>
        {
            input.WriteUInt32(1, InputSendKey);
            input.WriteUInt64(2, sessionId);
            input.WriteMessage(3, key =>
            {
                if (!string.IsNullOrEmpty(request.Text))
                {
                    WriteTextKey(key, request.Text);
                }
                else
                {
                    key.WriteUInt32(3, ToSpecialKey(request.Key));
                    if (RequiresShiftModifier(request.Key))
                    {
                        key.WriteUInt32(4, ModifierShift);
                    }
                }

                key.WriteUInt32(7, ModeHiragana);
                key.WriteBool(9, true);
            });
        });

    public static MozcIpcOutput ParseOutput(ReadOnlySpan<byte> commandBytes)
        => commandBytes.IsEmpty ? new MozcIpcOutput(0, false, string.Empty, [], -1, string.Empty) : ParseOutputMessage(commandBytes);

    private static byte[] WriteSessionCommand(ulong sessionId, uint commandType, uint? compositionMode = null) =>
        WriteInput(input =>
        {
            input.WriteUInt32(1, InputSendCommand);
            input.WriteUInt64(2, sessionId);
            input.WriteMessage(4, command =>
            {
                command.WriteUInt32(1, commandType);
                if (compositionMode is { } mode)
                {
                    command.WriteUInt32(3, mode);
                }
            });
        });

    private static byte[] WriteInput(Action<ProtoWriter> writeInput)
    {
        using var input = new ProtoWriter();
        writeInput(input);
        return input.ToArray();
    }

    private static void WriteTextKey(ProtoWriter key, string text)
    {
        if (text.Length == 1)
        {
            key.WriteUInt32(1, text[0]);
        }
        else
        {
            key.WriteString(5, text);
        }
    }

    private static uint ToSpecialKey(ImeBackendKey key) =>
        key switch
        {
            ImeBackendKey.Space => 4,
            ImeBackendKey.Enter => 5,
            ImeBackendKey.Escape => 10,
            ImeBackendKey.Backspace => 12,
            ImeBackendKey.Left or ImeBackendKey.SegmentWidthShrink => 6,
            ImeBackendKey.Right or ImeBackendKey.SegmentWidthExpand => 7,
            ImeBackendKey.Up => 8,
            ImeBackendKey.Down => 9,
            _ => 0,
        };

    private static bool RequiresShiftModifier(ImeBackendKey key) =>
        key is ImeBackendKey.SegmentWidthShrink or ImeBackendKey.SegmentWidthExpand;

    private static MozcIpcOutput ParseOutputMessage(ReadOnlySpan<byte> outputBytes)
    {
        ulong sessionId = 0;
        var consumed = false;
        var commitText = string.Empty;
        var preedit = string.Empty;
        var candidateWindow = CandidateParseResult.Empty;
        var allCandidates = CandidateParseResult.Empty;

        var reader = new ProtoReader(outputBytes);
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber, wireType)
            {
                case (1, 0):
                    sessionId = reader.ReadVarint();
                    break;
                case (3, 0):
                    consumed = reader.ReadVarint() != 0;
                    break;
                case (4, 2):
                    commitText = ParseResult(reader.ReadLengthDelimited());
                    break;
                case (5, 2):
                    preedit = ParsePreedit(reader.ReadLengthDelimited());
                    break;
                case (6, 2):
                    candidateWindow = ParseCandidateWindow(reader.ReadLengthDelimited());
                    break;
                case (14, 2):
                    allCandidates = ParseCandidateList(reader.ReadLengthDelimited());
                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        var selectedCandidates = candidateWindow.Candidates.Count > 0 ? candidateWindow : allCandidates;
        return new MozcIpcOutput(
            sessionId,
            consumed,
            preedit,
            selectedCandidates.Candidates,
            selectedCandidates.FocusedIndex,
            commitText);
    }

    private static string ParseResult(ReadOnlySpan<byte> bytes)
    {
        var reader = new ProtoReader(bytes);
        var value = string.Empty;
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 2 && wireType == 2)
            {
                value = reader.ReadString();
            }
            else
            {
                reader.SkipField(wireType);
            }
        }

        return value;
    }

    private static string ParsePreedit(ReadOnlySpan<byte> bytes)
    {
        var segments = new List<string>();
        var reader = new ProtoReader(bytes);
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 2 && wireType == 3)
            {
                segments.Add(ParsePreeditSegment(ref reader, fieldNumber));
            }
            else
            {
                reader.SkipField(wireType);
            }
        }

        return string.Concat(segments);
    }

    private static string ParsePreeditSegment(ref ProtoReader reader, int groupFieldNumber)
    {
        var value = string.Empty;
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            if (wireType == 4 && fieldNumber == groupFieldNumber)
            {
                return value;
            }

            if (fieldNumber == 4 && wireType == 2)
            {
                value = reader.ReadString();
            }
            else if (wireType == 3)
            {
                reader.SkipGroup(fieldNumber);
            }
            else
            {
                reader.SkipField(wireType);
            }
        }

        return value;
    }

    private static CandidateParseResult ParseCandidateWindow(ReadOnlySpan<byte> bytes)
    {
        var focusedIndex = -1;
        var candidates = new List<string>();
        var reader = new ProtoReader(bytes);
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber, wireType)
            {
                case (1, 0):
                    focusedIndex = checked((int)reader.ReadVarint());
                    break;
                case (3, 3):
                    var candidate = ParseCandidateWindowCandidate(ref reader, fieldNumber);
                    if (!string.IsNullOrEmpty(candidate))
                    {
                        candidates.Add(candidate);
                    }

                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        return new CandidateParseResult(candidates, focusedIndex);
    }

    private static string ParseCandidateWindowCandidate(ref ProtoReader reader, int groupFieldNumber)
    {
        var value = string.Empty;
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            if (wireType == 4 && fieldNumber == groupFieldNumber)
            {
                return value;
            }

            if (fieldNumber == 5 && wireType == 2)
            {
                value = reader.ReadString();
            }
            else if (wireType == 3)
            {
                reader.SkipGroup(fieldNumber);
            }
            else
            {
                reader.SkipField(wireType);
            }
        }

        return value;
    }

    private static CandidateParseResult ParseCandidateList(ReadOnlySpan<byte> bytes)
    {
        var focusedIndex = -1;
        var candidates = new List<string>();
        var reader = new ProtoReader(bytes);
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            switch (fieldNumber, wireType)
            {
                case (1, 0):
                    focusedIndex = checked((int)reader.ReadVarint());
                    break;
                case (2, 2):
                    var candidate = ParseCandidateWord(reader.ReadLengthDelimited());
                    if (!string.IsNullOrEmpty(candidate))
                    {
                        candidates.Add(candidate);
                    }

                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        return new CandidateParseResult(candidates, focusedIndex);
    }

    private static string ParseCandidateWord(ReadOnlySpan<byte> bytes)
    {
        var reader = new ProtoReader(bytes);
        var value = string.Empty;
        while (reader.TryReadField(out var fieldNumber, out var wireType))
        {
            if (fieldNumber == 4 && wireType == 2)
            {
                value = reader.ReadString();
            }
            else
            {
                reader.SkipField(wireType);
            }
        }

        return value;
    }

    private sealed record CandidateParseResult(IReadOnlyList<string> Candidates, int FocusedIndex)
    {
        public static CandidateParseResult Empty { get; } = new([], -1);
    }
}
