using PinguTools.Common.Localization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PinguTools.Services;

public sealed class ResourceService : IDisposable
{
    public ResourceService(string tempPath)
    {
        TempPath = tempPath;
        Directory.CreateDirectory(tempPath);
        var path = Environment.GetEnvironmentVariable("PATH");
        if (path == null) return;
        if (!path.Contains(tempPath)) Environment.SetEnvironmentVariable("PATH", $"{tempPath};{path}");
    }

    private string TempPath { get; }

    private Dictionary<string, string> RegisteredResources { get; } = [];

    public void Dispose()
    {
        foreach (var (key, value) in RegisteredResources.ToList())
        {
            try
            {
                RegisteredResources.Remove(key);
                File.Delete(value);
            }
            catch
            {
                // ignored
            }
        }
    }

    private static string? Find(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var isPathRelative = fileName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.PathSeparator, ':']) < 0;
        if (isPathRelative) return TrySearchPath(fileName);
        var candidate = Path.GetFullPath(fileName);
        return File.Exists(candidate) ? candidate : null;
    }

    public void Register(string fileName, byte[] resource)
    {
        var path = Path.Combine(TempPath, fileName);
        RegisteredResources.Add(fileName, path);

        if (Find(fileName) != null) return;

        try
        {
            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            fileStream.Write(resource, 0, resource.Length);
        }
        catch
        {
            RegisteredResources.Remove(fileName);
        }
    }

    public string GetRegisteredPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
        if (RegisteredResources.TryGetValue(fileName, out var path)) return path;
        var found = Find(fileName);
        if (found != null) return found;
        throw new FileNotFoundException(string.Format(CommonStrings.Error_file_not_found, fileName));
    }

    private static string? TrySearchPath(string fileName)
    {
        const int maxPath = 32767;
        var sb = new StringBuilder(maxPath);
        var hr = SearchPath(null, fileName, null, sb.Capacity, sb, IntPtr.Zero);
        return hr == 0 ? null : sb.ToString();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint SearchPath(string? lpPath, string lpFileName, string? lpExtension, int nBufferLength, StringBuilder lpBuffer, IntPtr lpFilePart);
}