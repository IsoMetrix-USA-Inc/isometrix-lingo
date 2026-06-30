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
            // Final validation before confirming
            if (viewModel.CanConfirm)
            {
                Close(viewModel.BranchConfigurations);
            }
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void DeployedBranch_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BranchComparisonViewModel viewModel && sender is TextBox textBox)
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                viewModel.ValidateBranchesCommand.Execute(null);
            }
        }
    }

    private void ReleaseBranch_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is BranchComparisonViewModel viewModel && sender is TextBox textBox)
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                viewModel.ValidateBranchesCommand.Execute(null);
            }
        }
    }
}
