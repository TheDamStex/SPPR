using SPPR.Helpers;

namespace SPPR.ViewModels;

public class FeatureViewModel : ObservableObject
{
    private bool _isSelected;

    public FeatureViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
