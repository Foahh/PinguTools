using PinguTools.Misc;
using System.ComponentModel;

namespace PinguTools.Attributes;

public class LocalizableCategoryAttribute(string categoryKey, Type resourceType) : CategoryAttribute(categoryKey)
{
    public Type ResourceType { get; } = resourceType;

    protected override string? GetLocalizedString(string value)
    {
        return ResourceType.GetPropertyValue(value, base.GetLocalizedString(value));
    }
}