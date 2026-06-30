using CommunityToolkit.Mvvm.ComponentModel;

namespace IsometrixLingo.ViewModels;

public partial class ProgressDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Working...";

    [ObservableProperty]
    private string _statusText = "Preparing...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PercentageText))]
    private double _percentage; // 0 - 100

    public string PercentageText => $"{Percentage:0}%";
}
