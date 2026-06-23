namespace IsometrixLingo.Models;

/// <summary>
/// Represents the type of change detected for a translation key via git diff.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// No change detected - key exists in both branches with same value.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Modified - key exists in both branches but value changed.
    /// </summary>
    Modified = 1,
    
    /// <summary>
    /// Added - key exists in target branch but not in base branch.
    /// </summary>
    Added = 2
}
