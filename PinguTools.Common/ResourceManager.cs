using PinguTools.Common.Resources;
using System.Runtime.InteropServices;
using System.Text;

namespace PinguTools.Common;

public static class ResourceManager
{
    private static readonly Lock Lock = new();
    public static string TempPath => Path.Combine(Path.GetTempPath(), Information.Name);
    private static Dictionary<string, string> Resources { get; } = new();

    public static void Initialize()
    {
        lock (Lock)
        {
            Directory.CreateDirectory(TempPath);
            var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (!path.Contains(TempPath, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH", $"{TempPath};{path}");
            }
        }

        Register("ebur128.dll", CommonResources.ebur128_dll);
    }

    public static void Release()
    {
        lock (Lock)
        {
            foreach (var filePath in Resources.Values)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            Resources.Clear();
            try
            {
                Directory.Delete(TempPath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    public static void Register(string fileName, byte[] resource)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
        if (resource == null || resource.Length == 0) throw new ArgumentNullException(nameof(resource));

        lock (Lock)
        {
            if (Resources.ContainsKey(fileName)) return;
            var finalPath = Path.Combine(TempPath, fileName);
            Resources[fileName] = finalPath;
            if (Find(fileName) != null) return;

            try
            {
                using var fileStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write);
                fileStream.Write(resource, 0, resource.Length);
            }
            catch
            {
                Resources.Remove(fileName);
                throw;
            }
        }
    }

    public static void Register(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(CommonStrings.Error_file_not_found, path);
        lock (Lock)
        {
            Resources.TryAdd(Path.GetFileName(path), path);
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