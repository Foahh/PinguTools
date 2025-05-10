using PinguTools.Chart.Localization;
using PinguTools.Common;
using System.Text.Json.Serialization;

namespace PinguTools.Chart.Models.c2s;

/*
   This model is based on the original implementation from:
   https://margrithm.girlsband.party/
*/

public abstract class Node
{
    public Time Tick { get; set; }

    public abstract string Id { get; }

    [JsonIgnore] public virtual string Text
    {
        get
        {
            var pos = Tick.Position;
            return $"{Id}\t{pos.Measure}\t{pos.Offset}";
        }
    }
}

public abstract class Note : Node
{
    public int Timeline { get; set; } = -1;

    public int Lane { get; set; }
    public int Width { get; set; }
    [JsonIgnore] public override string Text => $"{base.Text}\t{Lane}\t{Width}";
}

public abstract class LongNote : Note
{
    public Time Length { get; private set; }

    public int EndLane { get; set; }
    public int EndWidth { get; set; }

    public void SetLengthSafe(int value, IDiagnostic diag)
    {
        if (value < Time.SingleTick) diag.Report(DiagnosticSeverity.Warning, string.Format(Strings.Diag_set_length_smaller_than_unit, value, Time.SingleTick), Tick.Original, this);
        Length = Math.Max(Time.SingleTick, value);
    }
}

public abstract class LongHeightNote : LongNote
{
    public Height Height { get; set; }
    public Height EndHeight { get; set; }
}

public abstract class ExTapableLongNote : LongNote
{
    public ExEffect? Effect { get; set; }
}

public static class EffectExtension
{
    public static char GetMark(this ExEffect? e)
    {
        return e != null ? 'X' : 'L';
    }

    public static string GetKind(this ExEffect? e)
    {
        return e != null ? $"\t{e}" : "";
    }
}