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
}
