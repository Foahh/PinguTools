namespace PinguTools.Common;

public class DiagnosticException(string message, int? tick = null, object? target = null) : Exception(message)
{
    public object? Target { get; } = target;
    public int? Tick { get; set; } = tick;
}