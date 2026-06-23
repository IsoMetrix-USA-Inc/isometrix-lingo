using System;
using System.Collections.Generic;

namespace IsometrixLingo.Models;

/// <summary>
/// Metadata about change detection performed during extraction
/// </summary>
public class ChangeMetadata
{
    public string SchemaVersion { get; set; } = "1.0";
    public DateTime ExtractionTimestamp { get; set; } = DateTime.UtcNow;
    public List<RepositoryChangeInfo> Repositories { get; set; } = new();
}

/// <summary>
/// Information about changes detected in a single repository
/// </summary>
public class RepositoryChangeInfo
{
    public string Path { get; set; } = string.Empty;
    public string BaseBranch { get; set; } = string.Empty;
    public string TargetBranch { get; set; } = string.Empty;
    public string BaseCommit { get; set; } = string.Empty;
    public string TargetCommit { get; set; } = string.Empty;
    public List<FileChangeInfo> Files { get; set; } = new();
}

/// <summary>
/// Information about changes detected in a single translation file
/// </summary>
public class FileChangeInfo
{
    public string Path { get; set; } = string.Empty;
    public List<string> ModifiedKeys { get; set; } = new();
    public List<string> AddedKeys { get; set; } = new();
}
