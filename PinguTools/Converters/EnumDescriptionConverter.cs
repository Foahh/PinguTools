using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace PinguTools.Misc;

public sealed class EnumDescriptionConverter(Type type) : EnumConverter(type)
{
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType != typeof(string) || value == null) return base.ConvertTo(context, culture, value, destinationType);
        var fi = EnumType.GetField(value.ToString()!);
        var desc = fi?.GetCustomAttribute<DescriptionAttribute>();
        return desc?.Description ?? base.ConvertTo(context, culture, value, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        if (value is not string s) return base.ConvertFrom(context, culture, value!);
        var match = EnumType.GetFields().FirstOrDefault(f => f.GetCustomAttribute<DescriptionAttribute>()?.Description == s);
        if (match != null) return (Enum)match.GetValue(null)!;
        return Enum.Parse(EnumType, s, true);
    }
}