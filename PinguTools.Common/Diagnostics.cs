namespace PinguTools.Common;

public enum DiagnosticSeverity
{
    Information = 1,
    Warning = 2,
    Error = 3
}

public record Diagnostic(DiagnosticSeverity Severity, string Message, BarIndexCalculator.Position? Position = null, object? Target = null) : IComparable<Diagnostic>
{
    public int CompareTo(Diagnostic? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        var positionComparison = Comparer<BarIndexCalculator.Position?>.Default.Compare(Position, other.Position);
        if (positionComparison != 0) return positionComparison;
        var severityComparison = Severity.CompareTo(other.Severity);
        if (severityComparison != 0) return severityComparison;
        return string.Compare(Message, other.Message, StringComparison.Ordinal);
    }
}

public interface IDiagnostic
{
    bool HasProblems { get; }
    bool HasErrors { get; }

    BarIndexCalculator? BarCalculator { get; set; }
    void Report(Diagnostic diagnostic);
    void Report(DiagnosticSeverity severity, string message, int? tick = null, object? target = null);
}

public class DiagnosticReporter : IDiagnostic
{
    private readonly List<Diagnostic> diagnostics = [];
    public IReadOnlyCollection<Diagnostic> Diagnostics => diagnostics;

    public BarIndexCalculator? BarCalculator { get; set; }

    public bool HasProblems => diagnostics.Count > 0;
    public bool HasErrors => diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public void Report(Diagnostic diagnostic)
    {
        diagnostics.Add(diagnostic);
    }

    public void Report(DiagnosticSeverity severity, string message, int? tick = null, object? target = null)
    {
        var barPosition = tick != null ? BarCalculator?.GetPositionFromTick(tick.Value) : null;
        diagnostics.Add(new Diagnostic(severity, message, barPosition, target));
    }
}