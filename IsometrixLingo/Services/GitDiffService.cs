using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

public class GitDiffService
{
    private static readonly string LogFile = Path.Combine(Path.GetTempPath(), "isometrix-lingo-git-diff.log");

    public GitDiffService()
    {
        // Clear log file on startup
        try
        {
            File.WriteAllText(LogFile, $"=== Git Diff Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
            File.AppendAllText(LogFile, $"Log file location: {LogFile}\n\n");
        }
        catch { /* Ignore */ }
    }

    private void Log(string message)
    {
        try
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { /* Ignore logging errors */ }
    }
    /// <summary>
    /// Fetches the latest changes from the remote repository.
    /// </summary>
    /// <param name="repoPath">Absolute path to the git repository</param>
    /// <returns>True if fetch succeeded, false otherwise</returns>
    public async Task<bool> FetchRepositoryAsync(string repoPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var repo = new Repository(repoPath);

                // Fetch from all remotes
                foreach (var remote in repo.Network.Remotes)
                {
                    var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                    Commands.Fetch(repo, remote.Name, refSpecs, null, $"Fetch from {remote.Name}");
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        });
    }

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
        Log($"GetCommitHash: repo='{repoPath}', branch='{branchName}'");
        try
        {
            using var repo = new Repository(repoPath);
            var branch = repo.Branches[branchName];

            if (branch == null)
            {
                Log($"GetCommitHash: Branch '{branchName}' NOT FOUND");
                return null;
            }

            var sha = branch?.Tip?.Sha;
            Log($"GetCommitHash: Branch '{branchName}' → {(sha != null ? sha[..7] : "NULL")}");
            return sha;
        }
        catch (Exception ex)
        {
            Log($"GetCommitHash: ERROR - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the list of changed files between two branches.
    /// </summary>
    /// <param name="repoPath">Absolute path to the git repository</param>
    /// <param name="deployedBranch">Deployed branch name (e.g., "origin/main")</param>
    /// <param name="releaseBranch">Release branch name (e.g., "origin/release/v5.0.0")</param>
    /// <returns>List of changed file paths, or empty list if error occurs</returns>
    public List<string> GetChangedFiles(string repoPath, string deployedBranch, string releaseBranch)
    {
        Log($"GetChangedFiles: repo='{repoPath}', deployed='{deployedBranch}', release='{releaseBranch}'");
        var changedFiles = new List<string>();

        try
        {
            using var repo = new Repository(repoPath);

            var deployedCommit = repo.Branches[deployedBranch]?.Tip;
            var releaseCommit = repo.Branches[releaseBranch]?.Tip;

            if (deployedCommit == null)
            {
                Log($"GetChangedFiles: Deployed branch '{deployedBranch}' NOT FOUND");
                return changedFiles;
            }

            if (releaseCommit == null)
            {
                Log($"GetChangedFiles: Release branch '{releaseBranch}' NOT FOUND");
                return changedFiles;
            }

            Log($"GetChangedFiles: Comparing trees...");
            var changes = repo.Diff.Compare<TreeChanges>(deployedCommit.Tree, releaseCommit.Tree);

            foreach (var change in changes)
            {
                changedFiles.Add(change.Path);
                Log($"GetChangedFiles: Found changed file: {change.Path}");
            }

            Log($"GetChangedFiles: Returning {changedFiles.Count} changed file{(changedFiles.Count == 1 ? "" : "s")}");
            return changedFiles;
        }
        catch (Exception ex)
        {
            Log($"GetChangedFiles: ERROR - {ex.Message}");
            return changedFiles;
        }
    }

    /// <summary>
    /// Gets the diff content for a specific file between two branches.
    /// </summary>
    /// <param name="repoPath">Absolute path to the git repository</param>
    /// <param name="deployedBranch">Deployed branch name (e.g., \"origin/main\")</param>
    /// <param name="releaseBranch">Release branch name (e.g., \"origin/release/v5.0.0\")</param>
    /// <param name="filePath">Relative path to the file within the repository</param>
    /// <returns>Patch diff content, or null if error occurs</returns>
    public string? GetFileDiff(string repoPath, string deployedBranch, string releaseBranch, string filePath)
    {
        try
        {
            using var repo = new Repository(repoPath);

            var deployedCommit = repo.Branches[deployedBranch]?.Tip;
            var releaseCommit = repo.Branches[releaseBranch]?.Tip;

            if (deployedCommit == null)
            {
                Log($"[GitDiff] Deployed branch '{deployedBranch}' not found!");
                return null;
            }

            if (releaseCommit == null)
            {
                Log($"[GitDiff] Release branch '{releaseBranch}' not found!");
                return null;
            }

            Log($"[GitDiff] Comparing {deployedBranch} ({deployedCommit.Sha[..7]}) → {releaseBranch} ({releaseCommit.Sha[..7]}) for file: {filePath}");

            var patch = repo.Diff.Compare<Patch>(deployedCommit.Tree, releaseCommit.Tree, new[] { filePath });

            if (patch == null || string.IsNullOrWhiteSpace(patch.Content))
            {
                Log($"[GitDiff] NO CHANGES in {filePath}");
                return null;
            }

            Log($"[GitDiff] Found {patch.Content.Length} chars of diff for {filePath}");
            Log($"[GitDiff] Diff preview: {patch.Content.Substring(0, Math.Min(200, patch.Content.Length))}");

            return patch.Content;
        }
        catch (Exception ex)
        {
            Log($"[GitDiff] ERROR: {ex.Message}");
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
            Log("[ParseJsonDiff] Empty diff content");
            return changes;
        }

        Log($"[ParseJsonDiff] Parsing {diffContent.Length} chars of diff");

        // Parse git diff format:
        // Lines starting with - are removed
        // Lines starting with + are added
        // Format: "key": "value"

        var lines = diffContent.Split('\n');
        // Map key -> value signature for each side so we can tell a real change from a reorder
        var addedValues = new Dictionary<string, string>();
        var removedValues = new Dictionary<string, string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("+") && !trimmed.StartsWith("+++"))
            {
                var (key, value) = ExtractKeyValueFromJsonLine(trimmed.Substring(1).Trim());
                if (!string.IsNullOrEmpty(key))
                {
                    Log($"[ParseJsonDiff] Found ADDED line: {trimmed.Substring(0, Math.Min(80, trimmed.Length))} → key: {key}");
                    addedValues[key!] = value;
                }
            }
            else if (trimmed.StartsWith("-") && !trimmed.StartsWith("---"))
            {
                var (key, value) = ExtractKeyValueFromJsonLine(trimmed.Substring(1).Trim());
                if (!string.IsNullOrEmpty(key))
                {
                    Log($"[ParseJsonDiff] Found REMOVED line: {trimmed.Substring(0, Math.Min(80, trimmed.Length))} → key: {key}");
                    removedValues[key!] = value;
                }
            }
        }

        // Keys in both sets with the SAME value = reorder (skip)
        // Keys in both sets with a DIFFERENT value = Modified
        // Keys only in added = Added
        foreach (var (key, addedValue) in addedValues)
        {
            if (removedValues.TryGetValue(key, out var removedValue))
            {
                if (removedValue == addedValue)
                {
                    Log($"[ParseJsonDiff] → SKIPPED (reorder, value unchanged): {key}");
                    continue;
                }

                changes[key] = ChangeType.Modified;
                Log($"[ParseJsonDiff] → MODIFIED: {key}");
            }
            else
            {
                changes[key] = ChangeType.Added;
                Log($"[ParseJsonDiff] → ADDED: {key}");
            }
        }

        Log($"[ParseJsonDiff] Returning {changes.Count} change{(changes.Count == 1 ? "" : "s")}");
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
            Log("[ParseResxDiff] Empty diff content");
            return changes;
        }

        Log($"[ParseResxDiff] Parsing {diffContent.Length} chars of diff");

        // Parse RESX diff format
        // In RESX files, the <data name="key"> line doesn't change, but the <value> line does
        // So we need to track context: when we see a data name, remember it
        // Then when we see +/- value lines, attribute them to that key

        var lines = diffContent.Split('\n');
        // Map key -> value signature for each side so we can tell a real change from a reorder
        var addedValues = new Dictionary<string, string>();
        var removedValues = new Dictionary<string, string>();
        string? currentKey = null;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            // Look for <data name="key"> in context lines (no + or -)
            if (!trimmed.StartsWith("+") && !trimmed.StartsWith("-"))
            {
                var dataMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"<data\s+name=""([^""]+)""");
                if (dataMatch.Success)
                {
                    currentKey = dataMatch.Groups[1].Value;
                    Log($"[ParseResxDiff] Tracking key: {currentKey}");
                }
            }
            // Look for added <value> lines
            else if (trimmed.StartsWith("+") && !trimmed.StartsWith("+++"))
            {
                var content = trimmed.Substring(1).Trim();

                // Check if it's a <value> line
                if (content.Contains("<value>") && currentKey != null)
                {
                    Log($"[ParseResxDiff] Found ADDED value for key: {currentKey}");
                    AppendSignature(addedValues, currentKey, ExtractResxValueSignature(content));
                }
                // Or check if it's a <data name> line itself (new key added)
                else
                {
                    var key = ExtractKeyFromResxLine(content);
                    if (!string.IsNullOrEmpty(key))
                    {
                        Log($"[ParseResxDiff] Found ADDED data element: {key}");
                        if (!addedValues.ContainsKey(key!))
                        {
                            addedValues[key!] = string.Empty;
                        }
                        currentKey = key;
                    }
                }
            }
            // Look for removed <value> lines
            else if (trimmed.StartsWith("-") && !trimmed.StartsWith("---"))
            {
                var content = trimmed.Substring(1).Trim();

                // Check if it's a <value> line
                if (content.Contains("<value>") && currentKey != null)
                {
                    Log($"[ParseResxDiff] Found REMOVED value for key: {currentKey}");
                    AppendSignature(removedValues, currentKey, ExtractResxValueSignature(content));
                }
                // Or check if it's a <data name> line itself (key removed)
                else
                {
                    var key = ExtractKeyFromResxLine(content);
                    if (!string.IsNullOrEmpty(key))
                    {
                        Log($"[ParseResxDiff] Found REMOVED data element: {key}");
                        if (!removedValues.ContainsKey(key!))
                        {
                            removedValues[key!] = string.Empty;
                        }
                        currentKey = key;
                    }
                }
            }
        }

        // Keys in both sets with the SAME value = reorder (skip)
        // Keys in both sets with a DIFFERENT value = Modified
        // Keys only in added = Added
        foreach (var (key, addedValue) in addedValues)
        {
            if (removedValues.TryGetValue(key, out var removedValue))
            {
                if (removedValue == addedValue)
                {
                    Log($"[ParseResxDiff] → SKIPPED (reorder, value unchanged): {key}");
                    continue;
                }

                changes[key] = ChangeType.Modified;
                Log($"[ParseResxDiff] → MODIFIED: {key}");
            }
            else
            {
                changes[key] = ChangeType.Added;
                Log($"[ParseResxDiff] → ADDED: {key}");
            }
        }

        Log($"[ParseResxDiff] Returning {changes.Count} change{(changes.Count == 1 ? "" : "s")}");
        return changes;
    }

    private static void AppendSignature(Dictionary<string, string> signatures, string key, string value)
    {
        signatures[key] = signatures.TryGetValue(key, out var existing) && existing.Length > 0
            ? existing + "\n" + value
            : value;
    }

    private static string ExtractResxValueSignature(string line)
    {
        // Capture the inner text of <value>...</value> when present on a single line,
        // otherwise fall back to the whole line content (handles multi-line values).
        var match = System.Text.RegularExpressions.Regex.Match(line, @"<value>(.*?)</value>");
        return match.Success ? match.Groups[1].Value : line;
    }

    private (string? Key, string Value) ExtractKeyValueFromJsonLine(string line)
    {
        // Extract key and value from a JSON line: "key": "value"
        // The value signature is everything after the first colon (trailing comma stripped),
        // which lets us tell a real value change apart from a pure reorder.
        var keyMatch = System.Text.RegularExpressions.Regex.Match(line, @"""([^""]+)""\s*:");
        if (!keyMatch.Success)
        {
            return (null, string.Empty);
        }

        var key = keyMatch.Groups[1].Value;
        var afterColon = line.Substring(keyMatch.Index + keyMatch.Length).Trim();
        var value = afterColon.TrimEnd(',').Trim();
        return (key, value);
    }

    private string? ExtractKeyFromResxLine(string line)
    {
        // In RESX diffs, the <data name="..."> line defines the key
        // But often the VALUE is what changes, not the data line itself
        // So we look for: <data name="key" ...> OR <value>...</value>

        // Try to match <data name="key">
        var dataMatch = System.Text.RegularExpressions.Regex.Match(line, @"<data\s+name=""([^""]+)""");
        if (dataMatch.Success)
        {
            return dataMatch.Groups[1].Value;
        }

        // If it's a <value> line, we can't extract the key directly
        // The parser needs to track context
        return null;
    }
}
