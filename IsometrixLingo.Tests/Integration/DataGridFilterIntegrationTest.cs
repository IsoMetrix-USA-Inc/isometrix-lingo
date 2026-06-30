using IsometrixLingo.Models;
using IsometrixLingo.Services;
using System.IO;
using System.Linq;

namespace IsometrixLingo.Tests.Integration;

/// <summary>
/// Tests the COMPLETE workflow from import → git diff → filter → data grid
/// This tests what the user actually sees in the grid
/// </summary>
public class DataGridFilterIntegrationTest
{
    [Fact]
    public void CompleteWorkflow_WithFilter_ShouldShowModifiedKeysInGrid()
    {
        // Arrange - Setup the complete workflow
        var repoPath = "/Users/panospd/source/repos/iso/vcloud-web-api";
        var deployedBranch = "origin/main";
        var releaseBranch = "release/1.1.1";

        var gitDiffService = new GitDiffService();
        var translationStore = new TranslationStore();
        var resxReader = new ResxTranslationFileReader();

        // Step 1: Import files (simulate what happens in MainWindowViewModel.BulkImportFromDirectory)
        var resxFilePath = Path.Combine(repoPath, "IsoMetrix.Infrastructure/Translations/Resources/Emails/EmailTranslations.resx");
        var translationFile = resxReader.ReadFile(resxFilePath);

        // Set directory path on keys (this is what MainWindowViewModel does)
        foreach (var key in translationFile.Keys)
        {
            key.Source = new SourceFile(
                key.Source.Name,
                key.Source.Type,
                "IsoMetrix.Infrastructure/Translations/Resources/Emails"
            );
        }

        translationStore.AddTranslations(translationFile.Keys);

        Console.WriteLine($"Step 1: Loaded {translationStore.GetAllKeys().Count()} keys");

        // Step 2: Run git diff (simulate MainWindowViewModel.RunGitChangeDetection)
        var changedFiles = gitDiffService.GetChangedFiles(repoPath, deployedBranch, releaseBranch);
        var relativeFilePath = "IsoMetrix.Infrastructure/Translations/Resources/Emails/EmailTranslations.resx";
        var diffContent = gitDiffService.GetFileDiff(repoPath, deployedBranch, releaseBranch, relativeFilePath);
        var changes = gitDiffService.ParseResxDiff(diffContent!);

        Console.WriteLine($"Step 2: Git found {changes.Count} changed keys");

        // Apply changes to keys
        var allKeys = translationStore.GetAllKeys().ToList();
        foreach (var key in allKeys)
        {
            if (changes.TryGetValue(key.Key, out var changeType))
            {
                key.ChangeType = changeType;
                Console.WriteLine($"  Applied {changeType} to key: {key.Key}");
            }
        }

        // Verify at least one key has ChangeType set
        var keysWithChanges = allKeys.Where(k => k.ChangeType != ChangeType.None).ToList();
        Console.WriteLine($"Step 3: {keysWithChanges.Count} keys have ChangeType set");
        Assert.NotEmpty(keysWithChanges);

        // Step 4: Apply the "Only Modified/Added Keys" filter
        // This is what happens when user checks the filter checkbox
        translationStore.FilterByChangeType(true);

        // Step 5: Get filtered keys (this is what the DataGrid shows)
        var filteredKeys = translationStore.FilteredKeys.ToList();

        Console.WriteLine($"\n=== FINAL RESULT (what shows in grid) ===");
        Console.WriteLine($"Total keys: {allKeys.Count}");
        Console.WriteLine($"Keys with ChangeType: {keysWithChanges.Count}");
        Console.WriteLine($"Filtered keys (shown in grid): {filteredKeys.Count}");

        foreach (var key in filteredKeys)
        {
            Console.WriteLine($"  - {key.Key}: {key.ChangeType}");
        }

        // Assert - The grid should show the modified keys
        Assert.NotEmpty(filteredKeys);
        Assert.Contains(filteredKeys, k => k.Key == "Form__Completed__Subject");
        Assert.All(filteredKeys, k => Assert.True(k.ChangeType == ChangeType.Modified || k.ChangeType == ChangeType.Added));
    }

    [Fact]
    public void FilterByChangeType_WithNoChanges_ShouldShowAllKeys()
    {
        var translationStore = new TranslationStore();
        var key1 = new TranslationKey { Key = "test1", Source = new SourceFile("Test", FileType.Json) };
        var key2 = new TranslationKey { Key = "test2", Source = new SourceFile("Test", FileType.Json) };

        translationStore.AddTranslations(new[] { key1, key2 }.ToList());

        // When filter is OFF, should show all keys
        translationStore.FilterByChangeType(false);
        Assert.Equal(2, translationStore.FilteredKeys.Count());

        // When filter is ON but no changes, should show nothing
        translationStore.FilterByChangeType(true);
        var filtered = translationStore.FilteredKeys.ToList();
        Console.WriteLine($"Filtered count when filter=ON but no changes: {filtered.Count}");
        Assert.Empty(filtered);
    }

    [Fact]
    public void FilterByChangeType_WithChanges_ShouldShowOnlyChanged()
    {
        var translationStore = new TranslationStore();
        var key1 = new TranslationKey { Key = "test1", Source = new SourceFile("Test", FileType.Json), ChangeType = ChangeType.Modified };
        var key2 = new TranslationKey { Key = "test2", Source = new SourceFile("Test", FileType.Json), ChangeType = ChangeType.None };
        var key3 = new TranslationKey { Key = "test3", Source = new SourceFile("Test", FileType.Json), ChangeType = ChangeType.Added };

        translationStore.AddTranslations(new[] { key1, key2, key3 }.ToList());

        // Filter ON - should show only modified and added
        translationStore.FilterByChangeType(true);
        var filtered = translationStore.FilteredKeys.ToList();

        Console.WriteLine($"Filtered keys: {filtered.Count}");
        foreach (var k in filtered)
        {
            Console.WriteLine($"  - {k.Key}: {k.ChangeType}");
        }

        Assert.Equal(2, filtered.Count);
        Assert.Contains(filtered, k => k.Key == "test1");
        Assert.Contains(filtered, k => k.Key == "test3");
        Assert.DoesNotContain(filtered, k => k.Key == "test2");
    }
}
