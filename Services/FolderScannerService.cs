using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

public class FolderScannerService
{
    /// <summary>
    /// Scans a mod folder recursively for localization files
    /// </summary>
    public List<string> ScanLocalizationFiles(string modPath, string localizationFolder)
    {
        LoggingService.Log("INFO", $"Scanning folder: {modPath}");
        var ymlFiles = new List<string>();
        
        if (!Directory.Exists(modPath))
        {
            LoggingService.Log("WARNING", $"Folder does not exist: {modPath}");
            return ymlFiles;
        }

        ScanDirectory(modPath, localizationFolder, ymlFiles);
        LoggingService.Log("INFO", $"Found {ymlFiles.Count} localization files");
        return ymlFiles;
    }

    /// <summary>
    /// Recursively scans directories for .yml files in localization folders
    /// </summary>
    private void ScanDirectory(string path, string targetFolder, List<string> files)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirName = Path.GetFileName(dir);
                
                // Found localization folder
                if (dirName.Equals(targetFolder, StringComparison.OrdinalIgnoreCase))
                {
                    files.AddRange(Directory.GetFiles(dir, "*.yml", SearchOption.AllDirectories));
                }
                else
                {
                    // Continue searching in subdirectories
                    ScanDirectory(dir, targetFolder, files);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            LoggingService.Log("ERROR", $"Access denied: {path}", ex);
        }
        catch (Exception ex)
        {
            LoggingService.Log("ERROR", $"Error scanning {path}", ex);
        }
    }

    /// <summary>
    /// Filters files that belong to a specific source language
    /// </summary>
    public List<string> FilterSourceLanguageFiles(List<string> allFiles, string sourceLanguage, string localizationFolder)
    {
        return allFiles.Where(file =>
        {
            var parts = file.Split(Path.DirectorySeparatorChar);
            var langIndex = Array.IndexOf(parts, sourceLanguage);
            var localizationIndex = Array.FindIndex(parts, p => p.Equals(localizationFolder, StringComparison.OrdinalIgnoreCase));
            
            // File is in a subfolder named after the source language
            // OR file is at the root of localization folder and contains language in filename
            return langIndex != -1 || 
                   (localizationIndex != -1 && 
                    localizationIndex + 2 == parts.Length && 
                    file.Contains($"_{sourceLanguage}.yml", StringComparison.OrdinalIgnoreCase));
        }).ToList();
    }

    /// <summary>
    /// Finds missing target language files by comparing source files with existing files
    /// </summary>
    public List<string> FindMissingFiles(List<string> allFiles, List<string> sourceFiles, string sourceLanguage, string targetLanguage)
    {
        var targetFiles = sourceFiles.Select(f => 
            f.Replace($"{Path.DirectorySeparatorChar}{sourceLanguage}{Path.DirectorySeparatorChar}", 
                     $"{Path.DirectorySeparatorChar}{targetLanguage}{Path.DirectorySeparatorChar}")
             .Replace($"_{sourceLanguage}.yml", $"_{targetLanguage}.yml", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        return targetFiles.Where(tf => !allFiles.Contains(tf, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Performs a comprehensive scan and returns detailed results
    /// </summary>
    public FolderScanSummary PerformComprehensiveScan(
        string modPath, 
        string localizationFolder, 
        string sourceLanguage, 
        List<string> targetLanguages)
    {
        var summary = new FolderScanSummary
        {
            ModFolderPath = modPath
        };

        // Scan all files
        var allFiles = ScanLocalizationFiles(modPath, localizationFolder);
        summary.TotalFilesScanned = allFiles.Count;

        // Filter source language files
        var sourceFiles = FilterSourceLanguageFiles(allFiles, sourceLanguage, localizationFolder);
        summary.SourceLanguageFiles = sourceFiles.Count;

        // Check each target language
        foreach (var targetLang in targetLanguages)
        {
            var targetFiles = FilterSourceLanguageFiles(allFiles, targetLang, localizationFolder);
            summary.ExistingFilesByLanguage[targetLang] = targetFiles.Count;

            var missingFiles = FindMissingFiles(allFiles, sourceFiles, sourceLanguage, targetLang);
            summary.MissingFilesByLanguage[targetLang] = missingFiles.Count;

            // Add results
            foreach (var sourceFile in sourceFiles)
            {
                var correspondingTarget = sourceFile
                    .Replace($"{Path.DirectorySeparatorChar}{sourceLanguage}{Path.DirectorySeparatorChar}", 
                            $"{Path.DirectorySeparatorChar}{targetLang}{Path.DirectorySeparatorChar}")
                    .Replace($"_{sourceLanguage}.yml", $"_{targetLang}.yml", StringComparison.OrdinalIgnoreCase);

                summary.AllResults.Add(new ScanResult
                {
                    FilePath = correspondingTarget,
                    FileName = Path.GetFileName(correspondingTarget),
                    Language = targetLang,
                    Exists = File.Exists(correspondingTarget),
                    CorrespondingSourceFile = sourceFile
                });
            }
        }

        return summary;
    }

    /// <summary>
    /// Generates missing files from source files
    /// </summary>
    public List<string> GenerateMissingFiles(
        List<string> missingFiles, 
        string sourceLanguage, 
        string targetLanguage,
        GameConfig gameConfig)
    {
        var createdFiles = new List<string>();

        foreach (var targetFile in missingFiles)
        {
            try
            {
                var sourceFile = targetFile
                    .Replace($"{Path.DirectorySeparatorChar}{targetLanguage}{Path.DirectorySeparatorChar}", 
                            $"{Path.DirectorySeparatorChar}{sourceLanguage}{Path.DirectorySeparatorChar}")
                    .Replace($"_{targetLanguage}.yml", $"_{sourceLanguage}.yml", StringComparison.OrdinalIgnoreCase);

                if (!File.Exists(sourceFile))
                {
                    LoggingService.Log("WARNING", $"Source file not found: {sourceFile}");
                    continue;
                }

                // Read source content
                var sourceContent = File.ReadAllText(sourceFile);

                // Replace language keys
                var sourceKey = gameConfig.LanguageKeys.GetValueOrDefault(sourceLanguage, sourceLanguage);
                var targetKey = gameConfig.LanguageKeys.GetValueOrDefault(targetLanguage, targetLanguage);
                var targetContent = sourceContent.Replace($"l_{sourceKey}", $"l_{targetKey}");

                // Create directory if needed
                var targetDir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Write file
                File.WriteAllText(targetFile, targetContent);
                createdFiles.Add(targetFile);
                LoggingService.Log("INFO", $"Created: {targetFile}");
            }
            catch (Exception ex)
            {
                LoggingService.Log("ERROR", $"Error creating {targetFile}", ex);
            }
        }

        return createdFiles;
    }
}
