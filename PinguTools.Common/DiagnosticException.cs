namespace PinguTools.Common;

public class DiagnosticException : Exception
{
    public object? Target { get; }

    public DiagnosticException(string message, object? target = null) : base(message)
    {
        Target = target;
    }

    public DiagnosticException(string message, Exception innerException, object? target = null) : base(message, innerException)
    {
        Target = target;
    }
}