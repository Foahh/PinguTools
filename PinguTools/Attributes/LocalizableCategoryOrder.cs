using PinguTools.Misc;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PinguTools.Attributes;

public class LocalizableCategoryOrder : CategoryOrderAttribute
{
    public LocalizableCategoryOrder(string orderKey, int order, Type resourceType) : base(orderKey, order)
    {
        ResourceType = resourceType;
        var propertyInfo = typeof(CategoryOrderAttribute).GetProperty("CategoryValue");
        propertyInfo?.SetValue(this, ResourceType.GetPropertyValue(orderKey, orderKey));
    }

    public Type ResourceType { get; }
}