using System.Reflection;

namespace PinguTools.Common;

public static class Information
{
    public static readonly string Name = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new InvalidOperationException("Failed to retrieve application name");
    public static readonly Version Version = Assembly.GetEntryAssembly()?.GetName().Version ?? throw new InvalidOperationException("Failed to retrieve application version");
    public static readonly string VersionString = Version.ToString(3);
    public static readonly DateTime BuildDate = BuildDateAttribute.GetAssemblyBuildDate();
}