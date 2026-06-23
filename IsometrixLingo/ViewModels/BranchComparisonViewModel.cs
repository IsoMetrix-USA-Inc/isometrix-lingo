using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IsometrixLingo.Models;
using IsometrixLingo.Services;

namespace IsometrixLingo.ViewModels;

public partial class BranchComparisonViewModel : ViewModelBase
{
    private readonly GitDiffService _gitDiffService;
    private readonly List<string> _repositoryPaths;
    private int _currentRepositoryIndex;

    [ObservableProperty]
    private string _currentRepositoryName = string.Empty;

    [ObservableProperty]
    private string _currentRepositoryPath = string.Empty;

    [ObservableProperty]
    private string _baseBranch = "develop";

    [ObservableProperty]
    private string _targetBranch = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private int _currentRepositoryNumber;

    [ObservableProperty]
    private int _totalRepositories;

    [ObservableProperty]
    private bool _canNavigatePrevious;

    [ObservableProperty]
    private bool _canNavigateNext;

    [ObservableProperty]
    private bool _canConfirm;

    // Store validated branch configurations
    private readonly Dictionary<string, (string baseBranch, string targetBranch)> _branchConfigurations = new();

    public Dictionary<string, (string baseBranch, string targetBranch)> BranchConfigurations => _branchConfigurations;

    public BranchComparisonViewModel()
    {
        // Design-time constructor
        _gitDiffService = new GitDiffService();
        _repositoryPaths = new List<string>();
    }

    public BranchComparisonViewModel(List<DirectoryScanResult> selectedRepositories, GitDiffService gitDiffService)
    {
        _gitDiffService = gitDiffService;
        _repositoryPaths = selectedRepositories.Select(r => r.DirectoryPath).ToList();
        _currentRepositoryIndex = 0;
        TotalRepositories = _repositoryPaths.Count;

        if (_repositoryPaths.Count > 0)
        {
            LoadRepository(0);
        }

        UpdateNavigationState();
    }

    public BranchComparisonViewModel(List<string> repositoryPaths)
    {
        _gitDiffService = new GitDiffService();
        _repositoryPaths = repositoryPaths ?? new List<string>();
        _currentRepositoryIndex = 0;
        TotalRepositories = _repositoryPaths.Count;

        if (_repositoryPaths.Count > 0)
        {
            LoadRepository(0);
        }

        UpdateNavigationState();
    }

    private void LoadRepository(int index)
    {
        if (index < 0 || index >= _repositoryPaths.Count)
            return;

        _currentRepositoryIndex = index;
        CurrentRepositoryPath = _repositoryPaths[index];
        CurrentRepositoryName = System.IO.Path.GetFileName(CurrentRepositoryPath);
        CurrentRepositoryNumber = index + 1;

        // Load previously configured branches if they exist
        if (_branchConfigurations.TryGetValue(CurrentRepositoryPath, out var config))
        {
            BaseBranch = config.baseBranch;
            TargetBranch = config.targetBranch;
        }
        else
        {
            // Detect whether repository uses "main" or "master"
            BaseBranch = DetectDefaultBranch(CurrentRepositoryPath);
            TargetBranch = string.Empty;
        }

        ClearError();
        UpdateNavigationState();
    }

    private string DetectDefaultBranch(string repoPath)
    {
        // Check for "origin/main" first (modern convention with remote tracking)
        if (_gitDiffService.ValidateBranchExists(repoPath, "origin/main"))
        {
            return "origin/main";
        }

        // Fall back to "origin/master" (legacy convention with remote tracking)
        if (_gitDiffService.ValidateBranchExists(repoPath, "origin/master"))
        {
            return "origin/master";
        }

        // If neither remote tracking branch exists, default to "origin/main"
        return "origin/main";
    }

    [RelayCommand]
    private void NavigatePrevious()
    {
        if (_currentRepositoryIndex > 0)
        {
            // Save current configuration before navigating
            if (ValidateCurrentBranches())
            {
                SaveCurrentConfiguration();
                LoadRepository(_currentRepositoryIndex - 1);
            }
        }
    }

    [RelayCommand]
    private void NavigateNext()
    {
        if (_currentRepositoryIndex < _repositoryPaths.Count - 1)
        {
            // Save current configuration before navigating
            if (ValidateCurrentBranches())
            {
                SaveCurrentConfiguration();
                LoadRepository(_currentRepositoryIndex + 1);
            }
        }
    }

    [RelayCommand]
    private void ValidateBranches()
    {
        ValidateCurrentBranches();
        UpdateConfirmState();
    }

    private bool ValidateCurrentBranches()
    {
        ClearError();

        if (string.IsNullOrWhiteSpace(BaseBranch))
        {
            ShowError("Base branch name cannot be empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetBranch))
        {
            ShowError("Target branch name cannot be empty.");
            return false;
        }

        // Validate base branch exists
        if (!_gitDiffService.ValidateBranchExists(CurrentRepositoryPath, BaseBranch))
        {
            ShowError($"Branch '{BaseBranch}' not found in repository '{CurrentRepositoryName}'. Please verify the branch name.");
            return false;
        }

        // Validate target branch exists
        if (!_gitDiffService.ValidateBranchExists(CurrentRepositoryPath, TargetBranch))
        {
            ShowError($"Branch '{TargetBranch}' not found in repository '{CurrentRepositoryName}'. Please verify the branch name.");
            return false;
        }

        return true;
    }

    private void SaveCurrentConfiguration()
    {
        _branchConfigurations[CurrentRepositoryPath] = (BaseBranch, TargetBranch);
    }

    private void UpdateNavigationState()
    {
        CanNavigatePrevious = _currentRepositoryIndex > 0;
        CanNavigateNext = _currentRepositoryIndex < _repositoryPaths.Count - 1;
        UpdateConfirmState();
    }

    private void UpdateConfirmState()
    {
        // Can confirm if all repositories have been configured
        CanConfirm = _branchConfigurations.Count == _repositoryPaths.Count;
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    partial void OnBaseBranchChanged(string value)
    {
        ClearError();
    }

    partial void OnTargetBranchChanged(string value)
    {
        ClearError();
    }
}
