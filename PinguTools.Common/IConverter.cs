namespace PinguTools.Common;

public interface IConverter<in T>
{
    public Task ConvertAsync(T options, IDiagnostic diag, IProgress<string>? progress = null, CancellationToken ct = default);

    public bool CanConvert(T options, IDiagnostic diag);
}