using System.Text;
using MozcInput.Protocol;

namespace MozcInput.Bridge.Engine;

internal sealed class RomanKanaEngine : IBridgeEngine
{
    private static readonly Dictionary<string, string> RomajiMap = CreateRomajiMap();
    private readonly StringBuilder romanBuffer = new();
    private string preedit = string.Empty;
    private List<string> candidates = [];
    private int focusedCandidateIndex = -1;

    public MozcBridgeResponse Handle(MozcBridgeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Command switch
        {
            MozcBridgeCommand.CreateSession or MozcBridgeCommand.Reset => Reset(request.Sequence),
            MozcBridgeCommand.Shutdown => new MozcBridgeResponse(request.Sequence, Handled: true),
            MozcBridgeCommand.Cancel => Cancel(request.Sequence),
            MozcBridgeCommand.Commit => Commit(request.Sequence),
            MozcBridgeCommand.Key => HandleKey(request),
            _ => MozcBridgeResponse.PassThrough(request.Sequence),
        };
    }

    private MozcBridgeResponse HandleKey(MozcBridgeRequest request)
    {
        if (!string.IsNullOrEmpty(request.Text))
        {
            romanBuffer.Append(request.Text.ToUpperInvariant());
            RebuildPreedit();
            return Snapshot(request.Sequence);
        }

        return request.Key switch
        {
            MozcBridgeKey.Space => ConvertOrAdvanceCandidate(request.Sequence),
            MozcBridgeKey.Enter => Commit(request.Sequence),
            MozcBridgeKey.Escape => Cancel(request.Sequence),
            MozcBridgeKey.Up => MoveCandidate(request.Sequence, -1),
            MozcBridgeKey.Down => MoveCandidate(request.Sequence, 1),
            MozcBridgeKey.Backspace => Backspace(request.Sequence),
            MozcBridgeKey.Left
                or MozcBridgeKey.Right
                or MozcBridgeKey.SegmentWidthShrink
                or MozcBridgeKey.SegmentWidthExpand => HandleSegmentKey(request.Sequence),
            _ => MozcBridgeResponse.PassThrough(request.Sequence),
        };
    }

    private MozcBridgeResponse HandleSegmentKey(long sequence) =>
        string.IsNullOrEmpty(preedit) && candidates.Count == 0 ? MozcBridgeResponse.PassThrough(sequence) : Snapshot(sequence);

    private MozcBridgeResponse ConvertOrAdvanceCandidate(long sequence)
    {
        if (string.IsNullOrEmpty(preedit))
        {
            return MozcBridgeResponse.PassThrough(sequence);
        }

        if (candidates.Count == 0)
        {
            candidates = [preedit, $"{preedit}ー"];
            focusedCandidateIndex = 0;
        }
        else
        {
            focusedCandidateIndex = (focusedCandidateIndex + 1) % candidates.Count;
        }

        return Snapshot(sequence);
    }

    private MozcBridgeResponse MoveCandidate(long sequence, int delta)
    {
        if (candidates.Count == 0)
        {
            return MozcBridgeResponse.PassThrough(sequence);
        }

        focusedCandidateIndex = (focusedCandidateIndex + delta + candidates.Count) % candidates.Count;
        return Snapshot(sequence);
    }

    private MozcBridgeResponse Backspace(long sequence)
    {
        if (romanBuffer.Length == 0)
        {
            return MozcBridgeResponse.PassThrough(sequence);
        }

        romanBuffer.Length -= 1;
        RebuildPreedit();
        candidates.Clear();
        focusedCandidateIndex = -1;
        return Snapshot(sequence);
    }

    private MozcBridgeResponse Commit(long sequence)
    {
        if (string.IsNullOrEmpty(preedit) && candidates.Count == 0)
        {
            return MozcBridgeResponse.PassThrough(sequence);
        }

        var committed = candidates.Count > 0 && focusedCandidateIndex >= 0
            ? candidates[focusedCandidateIndex]
            : preedit;
        romanBuffer.Clear();
        preedit = string.Empty;
        candidates.Clear();
        focusedCandidateIndex = -1;
        return new MozcBridgeResponse(sequence, Handled: true, CommitText: committed);
    }

    private MozcBridgeResponse Cancel(long sequence)
    {
        romanBuffer.Clear();
        preedit = string.Empty;
        candidates.Clear();
        focusedCandidateIndex = -1;
        return new MozcBridgeResponse(sequence, Handled: true);
    }

    private MozcBridgeResponse Reset(long sequence)
    {
        romanBuffer.Clear();
        preedit = string.Empty;
        candidates.Clear();
        focusedCandidateIndex = -1;
        return Snapshot(sequence);
    }

    private MozcBridgeResponse Snapshot(long sequence) =>
        new(
            sequence,
            Handled: true,
            PreeditText: preedit,
            Candidates: candidates,
            FocusedCandidateIndex: focusedCandidateIndex);

    private void RebuildPreedit()
    {
        var source = romanBuffer.ToString();
        var builder = new StringBuilder();
        var index = 0;
        while (index < source.Length)
        {
            var matched = false;
            var maxLength = Math.Min(3, source.Length - index);
            for (var length = maxLength; length >= 1; length--)
            {
                var token = source.Substring(index, length);
                if (!RomajiMap.TryGetValue(token, out var kana))
                {
                    continue;
                }

                builder.Append(kana);
                index += length;
                matched = true;
                break;
            }

            if (!matched)
            {
                builder.Append(source[index]);
                index++;
            }
        }

        preedit = builder.ToString();
        candidates.Clear();
        focusedCandidateIndex = -1;
    }

    private static Dictionary<string, string> CreateRomajiMap() =>
        new(StringComparer.Ordinal)
        {
            ["A"] = "あ",
            ["I"] = "い",
            ["U"] = "う",
            ["E"] = "え",
            ["O"] = "お",
            ["KA"] = "か",
            ["KI"] = "き",
            ["KU"] = "く",
            ["KE"] = "け",
            ["KO"] = "こ",
            ["SA"] = "さ",
            ["SHI"] = "し",
            ["SU"] = "す",
            ["SE"] = "せ",
            ["SO"] = "そ",
            ["TA"] = "た",
            ["CHI"] = "ち",
            ["TSU"] = "つ",
            ["TE"] = "て",
            ["TO"] = "と",
            ["NA"] = "な",
            ["NI"] = "に",
            ["NU"] = "ぬ",
            ["NE"] = "ね",
            ["NO"] = "の",
            ["HA"] = "は",
            ["HI"] = "ひ",
            ["FU"] = "ふ",
            ["HE"] = "へ",
            ["HO"] = "ほ",
            ["MA"] = "ま",
            ["MI"] = "み",
            ["MU"] = "む",
            ["ME"] = "め",
            ["MO"] = "も",
            ["YA"] = "や",
            ["YU"] = "ゆ",
            ["YO"] = "よ",
            ["RA"] = "ら",
            ["RI"] = "り",
            ["RU"] = "る",
            ["RE"] = "れ",
            ["RO"] = "ろ",
            ["WA"] = "わ",
            ["WO"] = "を",
            ["N"] = "ん",
            ["GA"] = "が",
            ["GI"] = "ぎ",
            ["GU"] = "ぐ",
            ["GE"] = "げ",
            ["GO"] = "ご",
            ["ZA"] = "ざ",
            ["JI"] = "じ",
            ["ZU"] = "ず",
            ["ZE"] = "ぜ",
            ["ZO"] = "ぞ",
            ["DA"] = "だ",
            ["DE"] = "で",
            ["DO"] = "ど",
            ["BA"] = "ば",
            ["BI"] = "び",
            ["BU"] = "ぶ",
            ["BE"] = "べ",
            ["BO"] = "ぼ",
            ["PA"] = "ぱ",
            ["PI"] = "ぴ",
            ["PU"] = "ぷ",
            ["PE"] = "ぺ",
            ["PO"] = "ぽ",
            ["KYA"] = "きゃ",
            ["KYU"] = "きゅ",
            ["KYO"] = "きょ",
            ["SHA"] = "しゃ",
            ["SHU"] = "しゅ",
            ["SHO"] = "しょ",
            ["CHA"] = "ちゃ",
            ["CHU"] = "ちゅ",
            ["CHO"] = "ちょ",
            ["NYA"] = "にゃ",
            ["NYU"] = "にゅ",
            ["NYO"] = "にょ",
            ["HYA"] = "ひゃ",
            ["HYU"] = "ひゅ",
            ["HYO"] = "ひょ",
            ["MYA"] = "みゃ",
            ["MYU"] = "みゅ",
            ["MYO"] = "みょ",
            ["RYA"] = "りゃ",
            ["RYU"] = "りゅ",
            ["RYO"] = "りょ",
            ["GYA"] = "ぎゃ",
            ["GYU"] = "ぎゅ",
            ["GYO"] = "ぎょ",
            ["JA"] = "じゃ",
            ["JU"] = "じゅ",
            ["JO"] = "じょ",
            ["BYA"] = "びゃ",
            ["BYU"] = "びゅ",
            ["BYO"] = "びょ",
            ["PYA"] = "ぴゃ",
            ["PYU"] = "ぴゅ",
            ["PYO"] = "ぴょ",
        };
}
