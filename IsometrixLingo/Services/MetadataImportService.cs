using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

/// <summary>
/// Service for importing change detection metadata from JSON format
/// </summary>
public class MetadataImportService
{
    /// <summary>
    /// Checks if metadata.json exists in the specified directory
    /// </summary>
    public bool MetadataExists(string directoryPath)
    {
        var metadataPath = Path.Combine(directoryPath, "metadata.json");
        return File.Exists(metadataPath);
    }

    /// <summary>
    /// Loads and parses metadata.json from the specified directory
    /// Returns null if file doesn't exist or parsing fails
    /// </summary>
    public ChangeMetadata? LoadMetadata(string directoryPath)
    {
        var metadataPath = Path.Combine(directoryPath, "metadata.json");

        if (!File.Exists(metadataPath))
            return null;

        try
        {
            var json = File.ReadAllText(metadataPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<ChangeMetadata>(json, options);
        }
        catch (Exception)
        {
            // Invalid or corrupted metadata - return null
            return null;
        }
    }

    /// <summary>
    /// Applies change metadata to translation keys by matching key names and file paths
    /// </summary>
    public void ApplyMetadataToKeys(ChangeMetadata metadata, List<TranslationKey> keys, string rootDirectoryPath)
    {
        foreach (var repo in metadata.Repositories)
        {
            foreach (var file in repo.Files)
            {
                // file.Path includes the repo directory prefix (matching the imported
                // DirectoryPath), e.g.
                // "vcloud-web-api/IsoMetrix.Infrastructure/Translations/Resources/Emails/EmailTranslations.resx"
                var fileDir = (Path.GetDirectoryName(file.Path) ?? string.Empty).Replace('\\', '/');
                var fileName = Path.GetFileName(file.Path);

                // Find keys matching this file by comparing the directory (both include the
                // repo prefix) and the base file name
                var fileKeys = keys.Where(k =>
                {
                    if (k.Source?.DirectoryPath == null)
                        return false;

                    var normalizedDir = k.Source.DirectoryPath.Replace('\\', '/');

                    // Derive the base name from the changed file name (strip language + extension)
                    var baseName = k.Source.Type == FileType.Json
                        ? fileName.Split('.')[0]                     // Forms.es.json -> Forms
                        : fileName.Split('_')[0].Replace(".resx", ""); // Forms_es.resx -> Forms

                    var dirMatch = normalizedDir.Equals(fileDir, StringComparison.OrdinalIgnoreCase);
                    var nameMatch = baseName.Equals(k.Source.Name, StringComparison.OrdinalIgnoreCase);

                    return dirMatch && nameMatch;
                }).ToList();

                // Apply change types
                foreach (var key in fileKeys)
                {
                    if (file.ModifiedKeys.Contains(key.Key))
                    {
                        key.ChangeType = ChangeType.Modified;
                    }
                    else if (file.AddedKeys.Contains(key.Key))
                    {
                        key.ChangeType = ChangeType.Added;
                    }
                }
            }
        }
    }
}
