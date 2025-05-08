using Microsoft.Extensions.DependencyInjection;
using PinguTools.Common;
using PinguTools.Localization;
using PinguTools.Models;
using PinguTools.Services;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace PinguTools.Controls;

public partial class MetaPropertyGrid : PropertyGrid
{
    private readonly HashSet<PropertyDefinition> cache = [];

    private Type? target;

    public MetaPropertyGrid()
    {
        InitializeComponent();
    }

    public List<string> Whitelist { get; set; } = [];

    public AssetService AssetService => App.Services.GetRequiredService<AssetService>();

    private void SelectedObject_Changed(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var type = SelectedObject?.GetType();
        if (type != target)
        {
            target = type;
            GeneratePropertyDefinitions();
        }

        if (e.OldValue is WorkflowModel oldModel) oldModel.PropertyChanged -= Model_PropertyChanged;

        if (e.NewValue is WorkflowModel newModel)
        {
            newModel.PropertyChanged += Model_PropertyChanged;
            ControlPropertyVisibility(newModel);
        }
    }

    private void GeneratePropertyDefinitions()
    {
        if (target == null) return;
        PropertyDefinitions.Clear();

        var definitions = PropertyDefinitions.Where(def => def.TargetProperties != null);
        var existingProperties = definitions.SelectMany(def => def.TargetProperties.OfType<string>()).ToHashSet(StringComparer.Ordinal);
        var metaProps = target.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var hashSet = Whitelist.ToHashSet(StringComparer.Ordinal);

        for (var i = 0; i < metaProps.Length; i++)
        {
            var prop = metaProps[i];
            if (hashSet.Count > 0 && !hashSet.Contains(prop.Name)) continue;
            if (existingProperties.Contains(prop.Name)) continue;
            var order = prop.GetCustomAttribute<PropertyOrderAttribute>();
            var newDef = new PropertyDefinition
            {
                DisplayOrder = order?.Order ?? i, TargetProperties = new[] { prop.Name },
                Description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description,
                Category = prop.GetCustomAttribute<CategoryAttribute>()?.Category,
                DisplayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name,
                IsBrowsable = prop.GetCustomAttribute<BrowsableAttribute>()?.Browsable ?? true,
                IsExpandable = prop.GetCustomAttribute<ExpandableObjectAttribute>() != null
            };
            PropertyDefinitions.Add(newDef);
        }
    }

    private void Model_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not WorkflowModel model) return;
        if (e.PropertyName is nameof(WorkflowModel.Difficulty) or nameof(WorkflowModel.IsCustomBg)) ControlPropertyVisibility(model);
    }

    private void RestorePropertyGrid()
    {
        var currentDefs = new HashSet<PropertyDefinition>(PropertyDefinitions);
        var defsToAdd = cache.Where(def => !currentDefs.Contains(def)).ToList();
        foreach (var def in defsToAdd) PropertyDefinitions.Add(def);
        cache.Clear();
    }

    private void ControlPropertyVisibility(WorkflowModel? model)
    {
        if (model == null) return;
        RestorePropertyGrid();

        var isCustomBg = model.IsCustomBg;
        if (isCustomBg) HidePropertyDefinitions(nameof(WorkflowModel.Stage));
        else HidePropertyDefinitions(nameof(WorkflowModel.StageId), nameof(WorkflowModel.NotesFieldLine), nameof(WorkflowModel.BgFileName));

        var isWe = model.Difficulty == Difficulty.WorldsEnd;
        if (isWe) HidePropertyDefinitions(nameof(WorkflowModel.Level));
        else HideCategory(ModelStrings.Category_WorldsEnd);
    }

    private void HidePropertyDefinitions(params string[] names)
    {
        if (names.Length == 0) return;

        var nameSet = new HashSet<string>(names, StringComparer.Ordinal);
        var definitionsToRemove = PropertyDefinitions.Where(def => def.TargetProperties.Cast<string>().Any(nameSet.Contains)).ToList();

        foreach (var def in definitionsToRemove.Where(def => cache.Add(def)))
        {
            PropertyDefinitions.Remove(def);
        }
    }

    private void HideCategory(string category)
    {
        var defs = PropertyDefinitions.Where(def => category.Equals(def.Category, StringComparison.Ordinal)).ToList();
        foreach (var def in defs.Where(def => cache.Add(def))) PropertyDefinitions.Remove(def);
    }
}