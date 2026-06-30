using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IsometrixLingo.Models;
using IsometrixLingo.Services;

namespace IsometrixLingo.ViewModels;

public partial class BranchComparisonViewModel : ViewModelBase
{
    private const int ValidationDebounceMs = 500;

    private readonly GitDiffService _gitDiffService;
    private readonly List<string> _repositoryPaths;
    private int _currentRepositoryIndex;

    // Per-repository state (keyed by repository path)
    private readonly Dictionary<string, string> _deployedBranches = new();
    private readonly Dictionary<string, string> _releaseBranches = new();
    private readonly Dictionary<string, bool> _deployedValid = new();
    private readonly Dictionary<string, bool> _releaseValid = new();

    private bool _suppressValidation;
    private CancellationTokenSource? _deployedValidationCts;
    private CancellationTokenSource? _releaseValidationCts;

    [ObservableProperty]
    private string _currentRepositoryName = string.Empty;

    [ObservableProperty]
    private string _currentRepositoryPath = string.Empty;

    [ObservableProperty]
    private string _deployedBranch = string.Empty;

    [ObservableProperty]
    private string _releaseBranch = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDeployedBranchError))]
    private string _deployedBranchError = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReleaseBranchError))]
    private string _releaseBranchError = string.Empty;

    [ObservableProperty]
    private bool _isDeployedBranchValidating;

    [ObservableProperty]
    private bool _isReleaseBranchValidating;

    [ObservableProperty]
    private bool _isDeployedBranchValid;

    [ObservableProperty]
    private bool _isReleaseBranchValid;

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

    public bool HasDeployedBranchError => !string.IsNullOrEmpty(DeployedBranchError);

    public bool HasReleaseBranchError => !string.IsNullOrEmpty(ReleaseBranchError);

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
        : this(selectedRepositories.Select(r => r.DirectoryPath).ToList(), gitDiffService)
    {
    }

    public BranchComparisonViewModel(List<string> repositoryPaths)
        : this(repositoryPaths, new GitDiffService())
    {
    }

    private BranchComparisonViewModel(List<string> repositoryPaths, GitDiffService gitDiffService)
    {
        _gitDiffService = gitDiffService;
        _repositoryPaths = repositoryPaths ?? new List<string>();
        _currentRepositoryIndex = 0;
        TotalRepositories = _repositoryPaths.Count;

        // Seed default branch values for every repository so state is consistent up-front
        foreach (var path in _repositoryPaths)
        {
            _deployedBranches[path] = DetectDefaultBranch(path);
            _releaseBranches[path] = string.Empty;
            _deployedValid[path] = false;
            _releaseValid[path] = false;
        }

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

        // Populate fields from stored per-repository state without triggering debounced validation
        _suppressValidation = true;
        DeployedBranch = _deployedBranches.TryGetValue(CurrentRepositoryPath, out var deployed) ? deployed : string.Empty;
        ReleaseBranch = _releaseBranches.TryGetValue(CurrentRepositoryPath, out var release) ? release : string.Empty;
        _suppressValidation = false;

        DeployedBranchError = string.Empty;
        ReleaseBranchError = string.Empty;
        IsDeployedBranchValidating = false;
        IsReleaseBranchValidating = false;

        // Validate the current repository's branches immediately so its state is accurate
        ValidateAndApply(isDeployed: true, CurrentRepositoryPath, CurrentRepositoryName, DeployedBranch);
        ValidateAndApply(isDeployed: false, CurrentRepositoryPath, CurrentRepositoryName, ReleaseBranch);

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
            LoadRepository(_currentRepositoryIndex - 1);
        }
    }

    [RelayCommand]
    private void NavigateNext()
    {
        if (_currentRepositoryIndex < _repositoryPaths.Count - 1)
        {
            LoadRepository(_currentRepositoryIndex + 1);
        }
    }

    /// <summary>
    /// Validates a branch value against the repository and applies the result to both
    /// the observable UI state (if still on that repository) and the per-repository state.
    /// </summary>
    private void ValidateAndApply(bool isDeployed, string repoPath, string repoName, string branch)
    {
        var (valid, error) = ValidateBranchValue(repoPath, branch);

        if (isDeployed)
        {
            _deployedValid[repoPath] = valid;
        }
        else
        {
            _releaseValid[repoPath] = valid;
        }

        if (CurrentRepositoryPath == repoPath)
        {
            if (isDeployed)
            {
                IsDeployedBranchValidating = false;
                IsDeployedBranchValid = valid;
                DeployedBranchError = error;
            }
            else
            {
                IsReleaseBranchValidating = false;
                IsReleaseBranchValid = valid;
                ReleaseBranchError = error;
            }
        }

        UpdateConfirmState();
    }

    private (bool Valid, string Error) ValidateBranchValue(string repoPath, string branch)
    {
        if (string.IsNullOrWhiteSpace(branch))
        {
            // Required, but don't show a red error for an untouched empty field
            return (false, string.Empty);
        }

        try
        {
            if (_gitDiffService.ValidateBranchExists(repoPath, branch))
            {
                return (true, string.Empty);
            }

            return (false, $"Branch '{branch}' was not found in this repository. Check the name (e.g. include 'origin/' for remote branches).");
        }
        catch (Exception ex)
        {
            return (false, $"Could not validate branch: {ex.Message}");
        }
    }

    private void ScheduleValidation(bool isDeployed)
    {
        var repoPath = CurrentRepositoryPath;
        var repoName = CurrentRepositoryName;
        var branch = isDeployed ? DeployedBranch : ReleaseBranch;

        // Mark this branch as not-yet-valid while we wait, and clear any stale error
        if (isDeployed)
        {
            _deployedValid[repoPath] = false;
            DeployedBranchError = string.Empty;
            IsDeployedBranchValid = false;
            IsDeployedBranchValidating = !string.IsNullOrWhiteSpace(branch);
            _deployedValidationCts?.Cancel();
            _deployedValidationCts = new CancellationTokenSource();
        }
        else
        {
            _releaseValid[repoPath] = false;
            ReleaseBranchError = string.Empty;
            IsReleaseBranchValid = false;
            IsReleaseBranchValidating = !string.IsNullOrWhiteSpace(branch);
            _releaseValidationCts?.Cancel();
            _releaseValidationCts = new CancellationTokenSource();
        }

        UpdateConfirmState();

        var token = isDeployed ? _deployedValidationCts!.Token : _releaseValidationCts!.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ValidationDebounceMs, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                ValidateAndApply(isDeployed, repoPath, repoName, branch);
            });
        }, token);
    }

    private void UpdateNavigationState()
    {
        CanNavigatePrevious = _currentRepositoryIndex > 0;
        CanNavigateNext = _currentRepositoryIndex < _repositoryPaths.Count - 1;
        UpdateConfirmState();
    }

    private void UpdateConfirmState()
    {
        // Confirm is only allowed when EVERY repository has both branches provided and validated.
        // Identical rule whether there is 1 repository or many.
        CanConfirm = _repositoryPaths.Count > 0 && _repositoryPaths.All(path =>
            !string.IsNullOrWhiteSpace(_deployedBranches.GetValueOrDefault(path)) &&
            !string.IsNullOrWhiteSpace(_releaseBranches.GetValueOrDefault(path)) &&
            _deployedValid.GetValueOrDefault(path) &&
            _releaseValid.GetValueOrDefault(path));
    }

    /// <summary>
    /// Builds the final branch configuration map. Returns true only when every repository is valid.
    /// </summary>
    public bool TryBuildConfigurations()
    {
        if (!CanConfirm)
            return false;

        _branchConfigurations.Clear();
        foreach (var path in _repositoryPaths)
        {
            _branchConfigurations[path] = (_deployedBranches[path], _releaseBranches[path]);
        }

        return true;
    }

    partial void OnDeployedBranchChanged(string value)
    {
        if (_suppressValidation)
            return;

        _deployedBranches[CurrentRepositoryPath] = value;
        ScheduleValidation(isDeployed: true);
    }

    partial void OnReleaseBranchChanged(string value)
    {
        if (_suppressValidation)
            return;

        _releaseBranches[CurrentRepositoryPath] = value;
        ScheduleValidation(isDeployed: false);
    }
}

