using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PinguTools.Services;

namespace PinguTools.ViewModels;

public abstract class ViewModel : ObservableObject
{
    protected ViewModel(ActionService actions)
    {
        ActionService = actions;
        ActionService.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            ActionCommand?.NotifyCanExecuteChanged();
        };
    }

    protected ActionService ActionService { get; }
    public IRelayCommand? ActionCommand { get; protected init; }

    protected void RegisterCommandNotifier(IRelayCommand command)
    {
        ActionService.PropertyChanged += (_, e) =>
        {
            command?.NotifyCanExecuteChanged();
        };
    }
}