using IsometrixLingo.Services;
using IsometrixLingo.Models;

namespace IsometrixLingo.Tests.Services;

public class GitDiffServiceIntegrationTest
{
    [Fact]
    public void GetChangedFiles_RealRepo_ShouldDetectChanges()
    {
        // Arrange
        var service = new GitDiffService();
        var repoPath = "/Users/panospd/source/repos/iso/vcloud-web-api";
        var deployedBranch = "main";
        var releaseBranch = "release/1.1.1";
        
        // Act
        var changedFiles = service.GetChangedFiles(repoPath, deployedBranch, releaseBranch);
        
        // Assert
        Assert.NotEmpty(changedFiles);
        Assert.Contains(changedFiles, f => f.Contains("EmailTranslations.resx"));
        
        // Output for debugging
        Console.WriteLine($"Found {changedFiles.Count} changed files:");
        foreach (var file in changedFiles)
        {
            Console.WriteLine($"  - {file}");
        }
    }
    
    [Fact]
    public void GetFileDiff_RealRepo_ShouldReturnDiffContent()
    {
        // Arrange
        var service = new GitDiffService();
        var repoPath = "/Users/panospd/source/repos/iso/vcloud-web-api";
        var deployedBranch = "main";
        var releaseBranch = "release/1.1.1";
        var filePath = "IsoMetrix.Infrastructure/Translations/Resources/Emails/EmailTranslations.resx";
        
        // Act
        var diffContent = service.GetFileDiff(repoPath, deployedBranch, releaseBranch, filePath);
        
        // Assert
        Assert.NotNull(diffContent);
        Assert.NotEmpty(diffContent);
        
        // Output for debugging
        Console.WriteLine($"Diff content ({diffContent?.Length ?? 0} chars):");
        Console.WriteLine(diffContent);
    }
    
    [Fact]
    public void ParseResxDiff_RealRepo_ShouldDetectModifiedKey()
    {
        // Arrange
        var service = new GitDiffService();
        var repoPath = "/Users/panospd/source/repos/iso/vcloud-web-api";
        var deployedBranch = "main";
        var releaseBranch = "release/1.1.1";
        var filePath = "IsoMetrix.Infrastructure/Translations/Resources/Emails/EmailTranslations.resx";
        
        // Act
        var diffContent = service.GetFileDiff(repoPath, deployedBranch, releaseBranch, filePath);
        var changes = service.ParseResxDiff(diffContent!);
        
        // Assert
        Assert.NotEmpty(changes);
        Assert.True(changes.ContainsKey("Form__Completed__Subject"), "Should detect Form__Completed__Subject");
        Assert.Equal(ChangeType.Modified, changes["Form__Completed__Subject"]);
        
        // Output for debugging
        Console.WriteLine($"Found {changes.Count} changes:");
        foreach (var kvp in changes)
        {
            Console.WriteLine($"  - {kvp.Key}: {kvp.Value}");
        }
    }
    
    [Fact]
    public void GetCommitHash_RealRepo_ShouldReturnHashes()
    {
        // Arrange
        var service = new GitDiffService();
        var repoPath = "/Users/panospd/source/repos/iso/vcloud-web-api";
        var deployedBranch = "main";
        var releaseBranch = "release/1.1.1";
        
        // Act
        var deployedHash = service.GetCommitHash(repoPath, deployedBranch);
        var releaseHash = service.GetCommitHash(repoPath, releaseBranch);
        
        // Assert
        Assert.NotNull(deployedHash);
        Assert.NotNull(releaseHash);
        Assert.NotEqual(deployedHash, releaseHash);
        
        // Output for debugging
        Console.WriteLine($"Deployed ({deployedBranch}): {deployedHash}");
        Console.WriteLine($"Release ({releaseBranch}): {releaseHash}");
    }
}
