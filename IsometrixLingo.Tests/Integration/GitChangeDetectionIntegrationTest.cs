using IsometrixLingo.Models;
using IsometrixLingo.Services;
using System.IO;
using System.Linq;

namespace IsometrixLingo.Tests.Integration;

/// <summary>
/// End-to-end integration test for git change detection workflow
/// Tests the full flow: load files → detect changes → verify keys have ChangeType set
/// </summary>
public class GitChangeDetectionIntegrationTest
{
    [Fact]
    public void FullWorkflow_RealRepo_ShouldDetectChangesInKeys()
    {
        // Arrange
        var repoPath = "/Users/panospd/source/repos/iso/vcloud-web-api";
        var deployedBranch = "origin/main";
        var releaseBranch = "release/1.1.1";

        var gitDiffService = new GitDiffService();
        var translationStore = new TranslationStore();
        var resxReader = new ResxTranslationFileReader();

        // Load the actual RESX file from the repo
        var resxFilePath = Path.Combine(repoPath, "IsoMetrix.Infrastructure/Translations/Resources/Emails/EmailTranslations.resx");
        Assert.True(File.Exists(resxFilePath), $"RESX file not found: {resxFilePath}");

        var translationFile = resxReader.ReadFile(resxFilePath);

        // Set the directory path on each key (this is what MainWindowViewModel does during import)
        foreach (var key in translationFile.Keys)
        {
            key.Source = new SourceFile(
                key.Source.Name,
                key.Source.Type,
                "IsoMetrix.Infrastructure/Translations/Resources/Emails"
            );
        }

        // Add keys to translation store
        translationStore.AddTranslations(translationFile.Keys);

        var allKeys = translationStore.GetAllKeys().ToList();
        Console.WriteLine($"Loaded {allKeys.Count} keys from EmailTranslations.resx");

        // Verify the test key exists
        var testKey = allKeys.FirstOrDefault(k => k.Key == "Form__Completed__Subject");
        Assert.NotNull(testKey);
        Console.WriteLine($"Found test key: {testKey.Key}");

        // Act - Run git diff
        var changedFiles = gitDiffService.GetChangedFiles(repoPath, deployedBranch, releaseBranch);
        Console.WriteLine($"\nGit found {changedFiles.Count} changed files:");
        foreach (var file in changedFiles)
        {
            Console.WriteLine($"  - {file}");
        }

        // Get diff for the EmailTranslations.resx file
        var relativeFilePath = "IsoMetrix.Infrastructure/Translations/Resources/Emails/EmailTranslations.resx";
        var diffContent = gitDiffService.GetFileDiff(repoPath, deployedBranch, releaseBranch, relativeFilePath);

        Assert.NotNull(diffContent);
        Assert.NotEmpty(diffContent);
        Console.WriteLine($"\nDiff content: {diffContent.Length} chars");

        // Parse the diff
        var changes = gitDiffService.ParseResxDiff(diffContent);
        Console.WriteLine($"\nParsed {changes.Count} changes:");
        foreach (var kvp in changes)
        {
            Console.WriteLine($"  - {kvp.Key}: {kvp.Value}");
        }

        // Apply changes to keys (this is what MainWindowViewModel does)
        foreach (var key in allKeys)
        {
            if (changes.TryGetValue(key.Key, out var changeType))
            {
                key.ChangeType = changeType;
            }
        }

        // Assert - Verify the test key now has Modified status
        Assert.Equal(ChangeType.Modified, testKey.ChangeType);
        Console.WriteLine($"\n✓ Test key '{testKey.Key}' correctly marked as {testKey.ChangeType}");

        // Verify we can filter modified keys
        var modifiedKeys = allKeys.Where(k => k.ChangeType == ChangeType.Modified).ToList();
        Assert.NotEmpty(modifiedKeys);
        Console.WriteLine($"\n✓ Found {modifiedKeys.Count} modified key(s) total");
    }

    [Fact]
    public void DirectoryPathMatching_ShouldWork()
    {
        // This tests the specific matching logic that's failing
        var changedFilePath = "IsoMetrix.Infrastructure/Translations/Resources/Emails/EmailTranslations.resx";
        var sourceDirectoryPath = "IsoMetrix.Infrastructure/Translations/Resources/Emails";
        var sourceName = "EmailTranslations";

        var fileName = Path.GetFileName(changedFilePath);
        var fileDir = Path.GetDirectoryName(changedFilePath) ?? "";
        var baseName = fileName.Split('_')[0].Replace(".resx", "");

        Console.WriteLine($"Changed file: {changedFilePath}");
        Console.WriteLine($"  fileName: {fileName}");
        Console.WriteLine($"  fileDir: {fileDir}");
        Console.WriteLine($"  baseName: {baseName}");
        Console.WriteLine($"Source:");
        Console.WriteLine($"  name: {sourceName}");
        Console.WriteLine($"  dir: {sourceDirectoryPath}");

        var nameMatch = baseName.Equals(sourceName, StringComparison.OrdinalIgnoreCase);
        var dirMatch = fileDir.Equals(sourceDirectoryPath, StringComparison.OrdinalIgnoreCase);

        Console.WriteLine($"Match results: name={nameMatch}, dir={dirMatch}");

        Assert.True(nameMatch, "Name should match");
        Assert.True(dirMatch, "Directory should match");
    }
}
