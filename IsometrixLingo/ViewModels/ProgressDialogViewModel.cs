using CommunityToolkit.Mvvm.ComponentModel;

namespace IsometrixLingo.ViewModels;

public partial class ProgressDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Working...";

    [ObservableProperty]
    private string _statusText = "Please wait...";
}
