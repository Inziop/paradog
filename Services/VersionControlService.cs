using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services
{
    /// <summary>
    /// Service for managing file version history and snapshots
    /// Tracks different versions of game files across updates
    /// </summary>
    public class VersionControlService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ParadoxTranslator");
        
        private static readonly string VersionsFolder = Path.Combine(AppDataFolder, "Versions");

        static VersionControlService()
        {
            Directory.CreateDirectory(VersionsFolder);
        }

        /// <summary>
        /// Get the snapshots folder for a specific project
        /// </summary>
        private static string GetProjectVersionsFolder(string projectId)
        {
            var folder = Path.Combine(VersionsFolder, projectId);
            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Calculate MD5 hash of a file
        /// </summary>
        public static string CalculateFileHash(string filePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Calculate hash from entries list (for in-memory comparison)
        /// </summary>
        public static string CalculateEntriesHash(List<LocalizationEntry> entries)
        {
            try
            {
                var content = string.Join("|", entries.Select(e => $"{e.Key}:{e.SourceText}"));
                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Create a snapshot of a file version
        /// </summary>
        public static FileVersion CreateSnapshot(
            string projectId,
            string filePath, 
            string gameVersion,
            List<LocalizationEntry> entries,
            string? notes = null)
        {
            var snapshot = new FileVersion
            {
                Id = Guid.NewGuid().ToString(),
                FilePath = filePath,
                GameVersion = gameVersion,
                ImportDate = DateTime.Now,
                Entries = entries,
                FileHash = CalculateEntriesHash(entries),
                FileSize = File.Exists(filePath) ? new FileInfo(filePath).Length : 0,
                Notes = notes ?? string.Empty
            };

            SaveSnapshot(projectId, snapshot);
            return snapshot;
        }

        /// <summary>
        /// Save a snapshot to disk
        /// </summary>
        public static void SaveSnapshot(string projectId, FileVersion snapshot)
        {
            try
            {
                var folder = GetProjectVersionsFolder(projectId);
                var fileName = $"{Path.GetFileNameWithoutExtension(snapshot.FilePath)}_{snapshot.GameVersion}_{snapshot.Id}.json";
                var filePath = Path.Combine(folder, fileName);

                var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to save snapshot: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a specific snapshot
        /// </summary>
        public static FileVersion? LoadSnapshot(string projectId, string snapshotId)
        {
            try
            {
                var folder = GetProjectVersionsFolder(projectId);
                var files = Directory.GetFiles(folder, $"*_{snapshotId}.json");
                
                if (files.Length == 0)
                    return null;

                var json = File.ReadAllText(files[0], Encoding.UTF8);
                return JsonSerializer.Deserialize<FileVersion>(json);
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to load snapshot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all snapshots for a specific file in a project
        /// </summary>
        public static List<FileVersion> GetFileVersionHistory(string projectId, string fileName)
        {
            var versions = new List<FileVersion>();
            
            try
            {
                var folder = GetProjectVersionsFolder(projectId);
                var searchPattern = $"{Path.GetFileNameWithoutExtension(fileName)}_*.json";
                var files = Directory.GetFiles(folder, searchPattern);

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file, Encoding.UTF8);
                        var version = JsonSerializer.Deserialize<FileVersion>(json);
                        if (version != null)
                            versions.Add(version);
                    }
                    catch
                    {
                        // Skip corrupted files
                    }
                }

                // Sort by import date (newest first)
                return versions.OrderByDescending(v => v.ImportDate).ToList();
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to get version history: {ex.Message}");
                return versions;
            }
        }

        /// <summary>
        /// Get all snapshots for a project
        /// </summary>
        public static List<FileVersion> GetAllSnapshots(string projectId)
        {
            var snapshots = new List<FileVersion>();
            
            try
            {
                var folder = GetProjectVersionsFolder(projectId);
                var files = Directory.GetFiles(folder, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file, Encoding.UTF8);
                        var snapshot = JsonSerializer.Deserialize<FileVersion>(json);
                        if (snapshot != null)
                            snapshots.Add(snapshot);
                    }
                    catch
                    {
                        // Skip corrupted files
                    }
                }

                return snapshots.OrderByDescending(s => s.ImportDate).ToList();
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to get all snapshots: {ex.Message}");
                return snapshots;
            }
        }

        /// <summary>
        /// Get the latest snapshot for a file
        /// </summary>
        public static FileVersion? GetLatestSnapshot(string projectId, string fileName)
        {
            var history = GetFileVersionHistory(projectId, fileName);
            return history.FirstOrDefault();
        }

        /// <summary>
        /// Delete a snapshot
        /// </summary>
        public static void DeleteSnapshot(string projectId, string snapshotId)
        {
            try
            {
                var folder = GetProjectVersionsFolder(projectId);
                var files = Directory.GetFiles(folder, $"*_{snapshotId}.json");
                
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to delete snapshot: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a file has been modified since last snapshot
        /// </summary>
        public static bool HasFileChanged(string projectId, string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var latestSnapshot = GetLatestSnapshot(projectId, fileName);
                
                if (latestSnapshot == null)
                    return true; // No snapshot exists, consider it changed

                var currentHash = CalculateFileHash(filePath);
                return currentHash != latestSnapshot.FileHash;
            }
            catch
            {
                return true; // On error, assume changed
            }
        }

        /// <summary>
        /// Get comparison between current file and latest snapshot
        /// </summary>
        public static ComparisonResult? CompareWithLatestSnapshot(
            string projectId,
            string filePath,
            List<LocalizationEntry> currentEntries,
            Dictionary<string, string>? existingTranslations = null)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                var latestSnapshot = GetLatestSnapshot(projectId, fileName);
                
                if (latestSnapshot == null)
                    return null; // No snapshot to compare with

                // Create temporary snapshot for current version
                var currentSnapshot = new FileVersion
                {
                    FilePath = filePath,
                    GameVersion = "Current",
                    ImportDate = DateTime.Now,
                    Entries = currentEntries,
                    FileHash = CalculateEntriesHash(currentEntries)
                };

                return FileComparisonService.CompareVersions(
                    latestSnapshot, 
                    currentSnapshot, 
                    existingTranslations);
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to compare with latest snapshot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clean up old snapshots (keep only last N versions per file)
        /// </summary>
        public static void CleanupOldSnapshots(string projectId, int keepCount = 5)
        {
            try
            {
                var snapshots = GetAllSnapshots(projectId);
                var groupedByFile = snapshots
                    .GroupBy(s => Path.GetFileName(s.FilePath))
                    .ToList();

                foreach (var group in groupedByFile)
                {
                    var toDelete = group
                        .OrderByDescending(s => s.ImportDate)
                        .Skip(keepCount)
                        .ToList();

                    foreach (var snapshot in toDelete)
                    {
                        DeleteSnapshot(projectId, snapshot.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Error($"Failed to cleanup old snapshots: {ex.Message}");
            }
        }

        /// <summary>
        /// Export version history report
        /// </summary>
        public static string GenerateVersionHistoryReport(string projectId)
        {
            var report = new StringBuilder();
            var snapshots = GetAllSnapshots(projectId);
            var groupedByFile = snapshots.GroupBy(s => Path.GetFileName(s.FilePath));

            report.AppendLine("=================================================");
            report.AppendLine("VERSION HISTORY REPORT");
            report.AppendLine("=================================================");
            report.AppendLine();
            report.AppendLine($"Project ID: {projectId}");
            report.AppendLine($"Total Snapshots: {snapshots.Count}");
            report.AppendLine($"Files Tracked: {groupedByFile.Count()}");
            report.AppendLine();

            foreach (var group in groupedByFile)
            {
                report.AppendLine($"FILE: {group.Key}");
                report.AppendLine($"Versions: {group.Count()}");
                
                foreach (var version in group.OrderByDescending(v => v.ImportDate))
                {
                    report.AppendLine($"  • v{version.GameVersion} - {version.ImportDate:yyyy-MM-dd HH:mm}");
                    report.AppendLine($"    Entries: {version.Entries.Count}, Hash: {version.FileHash[..8]}...");
                    if (!string.IsNullOrWhiteSpace(version.Notes))
                        report.AppendLine($"    Notes: {version.Notes}");
                }
                report.AppendLine();
            }

            report.AppendLine("=================================================");
            return report.ToString();
        }
    }
}
