using Avalonia.Controls;
using Avalonia.Interactivity;
using IsometrixLingo.ViewModels;

namespace IsometrixLingo.Views;

public partial class BranchComparisonDialog : Window
{
    public BranchComparisonDialog()
    {
        InitializeComponent();
    }

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BranchComparisonViewModel viewModel)
        {
            // Only close with a result when every repository has valid branches
            if (viewModel.TryBuildConfigurations())
            {
                Close(viewModel.BranchConfigurations);
            }
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
