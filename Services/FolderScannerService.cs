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
        LoggingService.Log("INFO", $"Looking for localization folder: {localizationFolder}");
        var ymlFiles = new List<string>();
        
        if (!Directory.Exists(modPath))
        {
            LoggingService.Log("WARNING", $"Folder does not exist: {modPath}");
            return ymlFiles;
        }

        ScanDirectory(modPath, localizationFolder, ymlFiles);
        LoggingService.Log("INFO", $"Found {ymlFiles.Count} localization files");
        
        // Debug: Log first few files
        for (int i = 0; i < Math.Min(5, ymlFiles.Count); i++)
        {
            LoggingService.Log("INFO", $"Sample file {i + 1}: {ymlFiles[i]}");
        }
        
        return ymlFiles;
    }

    /// <summary>
    /// Recursively scans directories for .yml files in localization folders
    /// </summary>
    private void ScanDirectory(string path, string targetFolder, List<string> files)
    {
        try
        {
            // Check if current path contains the target localization folder
            if (path.Contains(targetFolder, StringComparison.OrdinalIgnoreCase))
            {
                // Add all .yml files in this directory
                files.AddRange(Directory.GetFiles(path, "*.yml", SearchOption.TopDirectoryOnly));
            }
            
            // Recursively scan subdirectories
            foreach (var dir in Directory.GetDirectories(path))
            {
                ScanDirectory(dir, targetFolder, files);
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
        List<string> targetLanguages,
        GameConfig gameConfig)
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
            if (gameConfig.UseOverrideMode)
            {
                // In override mode, we're not looking for target language files
                // Instead, we'll generate override files in the source language folder
                summary.ExistingFilesByLanguage[targetLang] = 0;
                summary.MissingFilesByLanguage[targetLang] = sourceFiles.Count; // All source files need override versions

                // Add results showing we need to create override versions of all source files
                foreach (var sourceFile in sourceFiles)
                {
                    summary.AllResults.Add(new ScanResult
                    {
                        FilePath = sourceFile, // Same path as source (override mode)
                        FileName = Path.GetFileName(sourceFile),
                        Language = targetLang,
                        Exists = false, // Mark as not existing so we generate them
                        CorrespondingSourceFile = sourceFile
                    });
                }
            }
            else
            {
                // Normal mode: look for target language files
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
        GameConfig gameConfig,
        string? outputModFolder = null)
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

                string actualTargetFile;
                string targetContent;
                
                if (gameConfig.UseOverrideMode)
                {
                    // Override mode: Create files in output mod folder structure
                    // Keep the same relative path structure but in the override language folder
                    
                    if (string.IsNullOrEmpty(outputModFolder))
                    {
                        LoggingService.Log("ERROR", "Override mode requires outputModFolder to be specified");
                        continue;
                    }
                    
                    // Find the localization folder in the source path
                    var localizationIndex = sourceFile.IndexOf(gameConfig.LocalizationFolder, StringComparison.OrdinalIgnoreCase);
                    if (localizationIndex == -1)
                    {
                        LoggingService.Log("WARNING", $"Could not find localization folder in path: {sourceFile}");
                        continue;
                    }
                    
                    // Get the relative path from localization folder onwards
                    var relativePath = sourceFile.Substring(localizationIndex);
                    
                    // Build output path: outputModFolder/localization/english/...
                    // Replace source language folder with override language folder
                    var sourceKey = gameConfig.LanguageKeys.GetValueOrDefault(sourceLanguage, sourceLanguage);
                    var overrideKey = gameConfig.LanguageKeys.GetValueOrDefault(gameConfig.OverrideLanguage, gameConfig.OverrideLanguage);
                    relativePath = relativePath.Replace($"{Path.DirectorySeparatorChar}{sourceKey}{Path.DirectorySeparatorChar}", 
                                                       $"{Path.DirectorySeparatorChar}{overrideKey}{Path.DirectorySeparatorChar}");
                    
                    actualTargetFile = Path.Combine(outputModFolder, relativePath);
                    
                    // Content keeps the override language key (l_english:) but text will be translated
                    // For now, we just create skeleton files with source content
                    targetContent = sourceContent; // Keep l_english: header for override mode
                    
                    LoggingService.Log("INFO", $"Override mode: Creating {actualTargetFile} to override {gameConfig.OverrideLanguage}");
                }
                else
                {
                    // Normal mode: Create new language folder (e.g., vietnamese/)
                    actualTargetFile = targetFile;
                    
                    // Replace language keys
                    var sourceKey = gameConfig.LanguageKeys.GetValueOrDefault(sourceLanguage, sourceLanguage);
                    var targetKey = gameConfig.LanguageKeys.GetValueOrDefault(targetLanguage, targetLanguage);
                    targetContent = sourceContent.Replace($"l_{sourceKey}", $"l_{targetKey}");
                }

                // Create directory if needed
                var targetDir = Path.GetDirectoryName(actualTargetFile);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                // Write file
                File.WriteAllText(actualTargetFile, targetContent);
                createdFiles.Add(actualTargetFile);
                LoggingService.Log("INFO", $"Created: {actualTargetFile}");
            }
            catch (UnauthorizedAccessException ex)
            {
                LoggingService.Log("ERROR", $"Permission denied: {targetFile}", ex);
            }
            catch (IOException ex)
            {
                LoggingService.Log("ERROR", $"IO error (disk full or file locked): {targetFile}", ex);
            }
            catch (Exception ex)
            {
                LoggingService.Log("ERROR", $"Error creating {targetFile}", ex);
            }
        }

        return createdFiles;
    }

    /// <summary>
    /// Generates a .mod descriptor file for a Paradox game mod
    /// </summary>
    public void GenerateModDescriptor(
        string modFolderPath,
        string modName,
        string targetLanguage,
        GameConfig gameConfig)
    {
        try
        {
            var modFileName = modName.Replace(" ", "_").ToLowerInvariant();
            var descriptorPath = Path.Combine(Path.GetDirectoryName(modFolderPath) ?? modFolderPath, $"{modFileName}.mod");
            
            var modContent = $@"name=""{modName}""
version=""1.0.0""
tags={{
    ""Translation""
    ""Localization""
}}
supported_version=""*""
path=""mod/{Path.GetFileName(modFolderPath)}""
";

            if (gameConfig.UseOverrideMode)
            {
                modContent += $@"
# This mod overrides {gameConfig.OverrideLanguage} localization files with {targetLanguage} translations
# Because {gameConfig.DisplayName} doesn't support custom languages, we override existing language files
";
            }

            File.WriteAllText(descriptorPath, modContent);
            LoggingService.Log("INFO", $"Created mod descriptor: {descriptorPath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            LoggingService.Log("ERROR", $"Permission denied creating mod descriptor: {modFolderPath}", ex);
        }
        catch (IOException ex)
        {
            LoggingService.Log("ERROR", $"IO error creating mod descriptor: {modFolderPath}", ex);
        }
        catch (Exception ex)
        {
            LoggingService.Log("ERROR", $"Error creating mod descriptor for {modFolderPath}", ex);
        }
    }
}
