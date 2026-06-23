using System;
using System.Collections.Generic;
using LibGit2Sharp;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

public class GitDiffService
{
    /// <summary>
    /// Validates that a branch exists in the specified repository.
    /// </summary>
    /// <param name="repoPath">Absolute path to the git repository</param>
    /// <param name="branchName">Name of the branch to validate</param>
    /// <returns>True if branch exists, false otherwise</returns>
    public bool ValidateBranchExists(string repoPath, string branchName)
    {
        try
        {
            using var repo = new Repository(repoPath);
            var branch = repo.Branches[branchName];
            return branch != null;
        }
        catch (RepositoryNotFoundException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the commit hash for the specified branch.
    /// </summary>
    /// <param name="repoPath">Absolute path to the git repository</param>
    /// <param name="branchName">Name of the branch</param>
    /// <returns>Commit SHA hash, or null if branch doesn't exist</returns>
    public string? GetCommitHash(string repoPath, string branchName)
    {
        try
        {
            using var repo = new Repository(repoPath);
            var branch = repo.Branches[branchName];
            return branch?.Tip?.Sha;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the list of changed files between two branches.
    /// </summary>
    /// <param name="repoPath">Absolute path to the git repository</param>
    /// <param name="baseBranch">Base branch name (e.g., "develop")</param>
    /// <param name="targetBranch">Target branch name (e.g., "release/v5.0.0")</param>
    /// <returns>List of changed file paths, or empty list if error occurs</returns>
    public List<string> GetChangedFiles(string repoPath, string baseBranch, string targetBranch)
    {
        var changedFiles = new List<string>();
        
        try
        {
            using var repo = new Repository(repoPath);
            
            var baseCommit = repo.Branches[baseBranch]?.Tip;
            var targetCommit = repo.Branches[targetBranch]?.Tip;
            
            if (baseCommit == null || targetCommit == null)
            {
                return changedFiles;
            }

            var changes = repo.Diff.Compare<TreeChanges>(baseCommit.Tree, targetCommit.Tree);
            
            foreach (var change in changes)
            {
                changedFiles.Add(change.Path);
            }
            
            return changedFiles;
        }
        catch (Exception)
        {
            return changedFiles;
        }
    }

    /// <summary>
    /// Gets the diff content for a specific file between two branches.
    /// </summary>
    /// <param name="repoPath">Absolute path to the git repository</param>
    /// <param name="baseBranch">Base branch name</param>
    /// <param name="targetBranch">Target branch name</param>
    /// <param name="filePath">Relative path to the file within the repository</param>
    /// <returns>Patch diff content, or null if error occurs</returns>
    public string? GetFileDiff(string repoPath, string baseBranch, string targetBranch, string filePath)
    {
        try
        {
            using var repo = new Repository(repoPath);
            
            var baseCommit = repo.Branches[baseBranch]?.Tip;
            var targetCommit = repo.Branches[targetBranch]?.Tip;
            
            if (baseCommit == null || targetCommit == null)
            {
                return null;
            }

            var patch = repo.Diff.Compare<Patch>(baseCommit.Tree, targetCommit.Tree, new[] { filePath });
            
            return patch?.Content;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Parses JSON diff output to extract modified and added keys.
    /// </summary>
    /// <param name="diffContent">Raw diff patch content</param>
    /// <returns>Dictionary mapping keys to ChangeType (Modified or Added)</returns>
    public Dictionary<string, ChangeType> ParseJsonDiff(string diffContent)
    {
        var changes = new Dictionary<string, ChangeType>();
        
        if (string.IsNullOrWhiteSpace(diffContent))
        {
            return changes;
        }

        // Parse git diff format:
        // Lines starting with - are removed
        // Lines starting with + are added
        // Format: "key": "value"
        
        var lines = diffContent.Split('\n');
        var addedKeys = new HashSet<string>();
        var removedKeys = new HashSet<string>();
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("+") && !trimmed.StartsWith("+++"))
            {
                var key = ExtractKeyFromJsonLine(trimmed.Substring(1).Trim());
                if (!string.IsNullOrEmpty(key))
                {
                    addedKeys.Add(key);
                }
            }
            else if (trimmed.StartsWith("-") && !trimmed.StartsWith("---"))
            {
                var key = ExtractKeyFromJsonLine(trimmed.Substring(1).Trim());
                if (!string.IsNullOrEmpty(key))
                {
                    removedKeys.Add(key);
                }
            }
        }
        
        // Keys in both sets = Modified
        // Keys only in added = Added
        foreach (var key in addedKeys)
        {
            if (removedKeys.Contains(key))
            {
                changes[key] = ChangeType.Modified;
            }
            else
            {
                changes[key] = ChangeType.Added;
            }
        }
        
        return changes;
    }

    /// <summary>
    /// Parses RESX diff output to extract modified and added keys.
    /// </summary>
    /// <param name="diffContent">Raw diff patch content</param>
    /// <returns>Dictionary mapping keys to ChangeType (Modified or Added)</returns>
    public Dictionary<string, ChangeType> ParseResxDiff(string diffContent)
    {
        var changes = new Dictionary<string, ChangeType>();
        
        if (string.IsNullOrWhiteSpace(diffContent))
        {
            return changes;
        }

        // Parse RESX diff format
        // Look for <data name="key"> elements
        
        var lines = diffContent.Split('\n');
        var addedKeys = new HashSet<string>();
        var removedKeys = new HashSet<string>();
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            if (trimmed.StartsWith("+") && !trimmed.StartsWith("+++"))
            {
                var key = ExtractKeyFromResxLine(trimmed.Substring(1).Trim());
                if (!string.IsNullOrEmpty(key))
                {
                    addedKeys.Add(key);
                }
            }
            else if (trimmed.StartsWith("-") && !trimmed.StartsWith("---"))
            {
                var key = ExtractKeyFromResxLine(trimmed.Substring(1).Trim());
                if (!string.IsNullOrEmpty(key))
                {
                    removedKeys.Add(key);
                }
            }
        }
        
        // Keys in both sets = Modified
        // Keys only in added = Added
        foreach (var key in addedKeys)
        {
            if (removedKeys.Contains(key))
            {
                changes[key] = ChangeType.Modified;
            }
            else
            {
                changes[key] = ChangeType.Added;
            }
        }
        
        return changes;
    }

    private string? ExtractKeyFromJsonLine(string line)
    {
        // Extract key from JSON line: "key": "value"
        var match = System.Text.RegularExpressions.Regex.Match(line, @"""([^""]+)""\s*:");
        return match.Success ? match.Groups[1].Value : null;
    }

    private string? ExtractKeyFromResxLine(string line)
    {
        // Extract key from RESX line: <data name="key" xml:space="preserve">
        var match = System.Text.RegularExpressions.Regex.Match(line, @"<data\s+name=""([^""]+)""");
        return match.Success ? match.Groups[1].Value : null;
    }
}
