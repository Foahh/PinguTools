using System.Diagnostics.CodeAnalysis;

namespace PinguTools.Common;

public enum DiagnosticSeverity
{
    Information = 1,
    Warning = 2,
    Error = 3
}

public record Diagnostic(DiagnosticSeverity Severity, string Message, object? Target = null);

public interface IDiagnostic
{
    bool HasProblems { get; }
    bool HasErrors { get; }

    void Report(Diagnostic diagnostic);
    void Report(DiagnosticSeverity severity, string message, object? target = null);
}

public class DiagnosticReporter : IDiagnostic
{
    private readonly List<Diagnostic> diagnostics = [];
    public IReadOnlyCollection<Diagnostic> Diagnostics => diagnostics;

    public bool HasProblems => diagnostics.Count > 0;
    public bool HasErrors => diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    public void Report(Diagnostic diagnostic)
    {
        diagnostics.Add(diagnostic);
    }

    public void Report(DiagnosticSeverity severity, string message, object? target = null)
    {
        diagnostics.Add(new Diagnostic(severity, message, target));
    }
}