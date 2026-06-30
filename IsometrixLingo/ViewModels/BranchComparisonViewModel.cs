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
    private string _deployedBranch = string.Empty;

    [ObservableProperty]
    private string _releaseBranch = string.Empty;

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

    public string ConfirmButtonText => TotalRepositories == 1 ? "Done" : "Confirm All";
    
    public bool ShowNavigationButtons => TotalRepositories > 1;

    // Store validated branch configurations
    private readonly Dictionary<string, (string deployedBranch, string releaseBranch)> _branchConfigurations = new();

    public Dictionary<string, (string deployedBranch, string releaseBranch)> BranchConfigurations => _branchConfigurations;

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
            DeployedBranch = config.deployedBranch;
            ReleaseBranch = config.releaseBranch;
        }
        else
        {
            // Detect whether repository uses "origin/main" or "origin/master"
            DeployedBranch = DetectDefaultBranch(CurrentRepositoryPath);
            ReleaseBranch = string.Empty;
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

        // Check for local "main" branch
        if (_gitDiffService.ValidateBranchExists(repoPath, "main"))
        {
            return "main";
        }
        
        // Check for local "master" branch
        if (_gitDiffService.ValidateBranchExists(repoPath, "master"))
        {
            return "master";
        }
        
        // If nothing found, default to "origin/main" (user will get validation error if wrong)
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
        if (ValidateCurrentBranches())
        {
            // Save configuration if validation succeeds
            SaveCurrentConfiguration();
            UpdateConfirmState();
        }
    }

    private bool ValidateCurrentBranches()
    {
        ClearError();

        if (string.IsNullOrWhiteSpace(DeployedBranch))
        {
            ShowError("Deployed branch name cannot be empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ReleaseBranch))
        {
            ShowError("Release branch name cannot be empty.");
            return false;
        }

        // Validate deployed branch exists
        if (!_gitDiffService.ValidateBranchExists(CurrentRepositoryPath, DeployedBranch))
        {
            ShowError($"Branch '{DeployedBranch}' not found in repository '{CurrentRepositoryName}'. Please verify the branch name.");
            return false;
        }

        // Validate release branch exists
        if (!_gitDiffService.ValidateBranchExists(CurrentRepositoryPath, ReleaseBranch))
        {
            ShowError($"Branch '{ReleaseBranch}' not found in repository '{CurrentRepositoryName}'. Please verify the branch name.");
            return false;
        }

        return true;
    }

    private void SaveCurrentConfiguration()
    {
        _branchConfigurations[CurrentRepositoryPath] = (DeployedBranch, ReleaseBranch);
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

    partial void OnDeployedBranchChanged(string value)
    {
        ClearError();
    }

    partial void OnReleaseBranchChanged(string value)
    {
        ClearError();
    }
}
