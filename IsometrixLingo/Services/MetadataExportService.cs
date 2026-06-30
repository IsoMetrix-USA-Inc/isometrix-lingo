using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IsometrixLingo.Models;

namespace IsometrixLingo.Services;

/// <summary>
/// Service for exporting change detection metadata to JSON format
/// </summary>
public class MetadataExportService
{
    /// <summary>
    /// Generates metadata.json content from change metadata
    /// </summary>
    public string GenerateMetadata(ChangeMetadata changeMetadata)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(changeMetadata, options);
    }

    /// <summary>
    /// Writes metadata.json to the specified directory
    /// </summary>
    public void WriteMetadataFile(string directoryPath, ChangeMetadata changeMetadata)
    {
        var metadataJson = GenerateMetadata(changeMetadata);
        var filePath = Path.Combine(directoryPath, "metadata.json");
        File.WriteAllText(filePath, metadataJson);
    }

    /// <summary>
    /// Updates each file's ApprovedKeys list to reflect the current approval state of the live keys.
    /// Modified/Added key lists are left untouched - approval is tracked as a separate property.
    /// </summary>
    public void SyncApprovedKeys(ChangeMetadata changeMetadata, IEnumerable<TranslationKey> keys)
    {
        var keyList = keys.ToList();

        foreach (var repo in changeMetadata.Repositories)
        {
            foreach (var file in repo.Files)
            {
                var fileDir = (Path.GetDirectoryName(file.Path) ?? string.Empty).Replace('\\', '/');
                var fileName = Path.GetFileName(file.Path);

                // Find live keys belonging to this file (same matching as import side)
                var fileKeys = keyList.Where(k =>
                {
                    if (k.Source?.DirectoryPath == null)
                        return false;

                    var normalizedDir = k.Source.DirectoryPath.Replace('\\', '/');

                    var baseName = k.Source.Type == FileType.Json
                        ? fileName.Split('.')[0]                        // Forms.es.json -> Forms
                        : fileName.Split('_')[0].Replace(".resx", "");  // Forms_es.resx -> Forms

                    var dirMatch = normalizedDir.Equals(fileDir, StringComparison.OrdinalIgnoreCase);
                    var nameMatch = baseName.Equals(k.Source.Name, StringComparison.OrdinalIgnoreCase);

                    return dirMatch && nameMatch;
                });

                // Recompute approved keys for this file from current state
                file.ApprovedKeys = fileKeys
                    .Where(k => k.IsApproved
                        && (file.ModifiedKeys.Contains(k.Key) || file.AddedKeys.Contains(k.Key)))
                    .Select(k => k.Key)
                    .Distinct()
                    .ToList();
            }
        }
    }
}
