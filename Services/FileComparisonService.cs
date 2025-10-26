using System;
using System.Collections.Generic;
using System.Linq;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services
{
    /// <summary>
    /// Service for comparing different versions of localization files
    /// Used to detect changes when game updates and preserve existing translations
    /// </summary>
    public class FileComparisonService
    {
        /// <summary>
        /// Compare two file versions and detect changes
        /// </summary>
        public static ComparisonResult CompareVersions(
            FileVersion oldVersion, 
            FileVersion newVersion,
            Dictionary<string, string>? existingTranslations = null)
        {
            var result = new ComparisonResult
            {
                OldVersion = oldVersion,
                NewVersion = newVersion,
                ComparisonDate = DateTime.Now
            };

            // Create dictionaries for fast lookup
            var oldEntries = oldVersion.Entries.ToDictionary(e => e.Key, e => e);
            var newEntries = newVersion.Entries.ToDictionary(e => e.Key, e => e);
            
            // Get all unique keys from both versions
            var allKeys = oldEntries.Keys.Union(newEntries.Keys).OrderBy(k => k).ToList();

            foreach (var key in allKeys)
            {
                var hasOld = oldEntries.TryGetValue(key, out var oldEntry);
                var hasNew = newEntries.TryGetValue(key, out var newEntry);
                
                var compEntry = new ComparisonEntry
                {
                    Key = key,
                    OldEntry = oldEntry,
                    NewEntry = newEntry
                };

                // Get existing translation if available
                if (existingTranslations != null && existingTranslations.TryGetValue(key, out var translation))
                {
                    compEntry.ExistingTranslation = translation;
                    compEntry.UpdatedTranslation = translation; // Default to existing
                }

                // Determine change type
                if (!hasOld && hasNew)
                {
                    // New key added
                    compEntry.ChangeType = ChangeType.New;
                    compEntry.UpdatedTranslation = string.Empty; // Needs translation
                    compEntry.StatusMessage = "New entry - needs translation";
                }
                else if (hasOld && !hasNew)
                {
                    // Key deleted
                    compEntry.ChangeType = ChangeType.Deleted;
                    compEntry.StatusMessage = "Deleted in new version";
                }
                else if (hasOld && hasNew)
                {
                    // Check if source text changed
                    var oldText = oldEntry?.SourceText?.Trim() ?? string.Empty;
                    var newText = newEntry?.SourceText?.Trim() ?? string.Empty;
                    
                    if (oldText != newText)
                    {
                        // Source text modified
                        compEntry.ChangeType = ChangeType.Modified;
                        compEntry.StatusMessage = "Source text changed - review translation";
                    }
                    else
                    {
                        // Unchanged
                        compEntry.ChangeType = ChangeType.Unchanged;
                        compEntry.StatusMessage = "No changes";
                    }
                }

                result.Entries.Add(compEntry);
            }

            return result;
        }

        /// <summary>
        /// Apply comparison results and generate updated entries
        /// </summary>
        public static List<LocalizationEntry> ApplyComparisonResults(ComparisonResult comparison)
        {
            var updatedEntries = new List<LocalizationEntry>();

            foreach (var compEntry in comparison.Entries)
            {
                // Skip deleted entries
                if (compEntry.ChangeType == ChangeType.Deleted)
                    continue;

                // Use new entry as base (or old if new doesn't exist)
                var baseEntry = compEntry.NewEntry ?? compEntry.OldEntry;
                if (baseEntry == null) continue;

                var updatedEntry = new LocalizationEntry
                {
                    Key = compEntry.Key,
                    SourceText = baseEntry.SourceText,
                    TranslatedText = compEntry.UpdatedTranslation,
                    RawLineBefore = baseEntry.RawLineBefore,
                    RawLineAfter = baseEntry.RawLineAfter
                };

                updatedEntries.Add(updatedEntry);
            }

            return updatedEntries;
        }

        /// <summary>
        /// Generate a text report of the comparison
        /// </summary>
        public static string GenerateReport(ComparisonResult comparison)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=================================================");
            report.AppendLine("FILE COMPARISON REPORT");
            report.AppendLine("=================================================");
            report.AppendLine();
            report.AppendLine($"Old Version: {comparison.OldVersion.GameVersion} ({comparison.OldVersion.ImportDate:yyyy-MM-dd})");
            report.AppendLine($"New Version: {comparison.NewVersion.GameVersion} ({comparison.NewVersion.ImportDate:yyyy-MM-dd})");
            report.AppendLine($"Comparison Date: {comparison.ComparisonDate:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();
            report.AppendLine("SUMMARY:");
            report.AppendLine($"  🟢 New Entries:       {comparison.NewCount,5}");
            report.AppendLine($"  🟡 Modified Entries:  {comparison.ModifiedCount,5}");
            report.AppendLine($"  🔴 Deleted Entries:   {comparison.DeletedCount,5}");
            report.AppendLine($"  ⚪ Unchanged Entries: {comparison.UnchangedCount,5}");
            report.AppendLine($"  ━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            report.AppendLine($"  📊 Total:             {comparison.TotalCount,5}");
            report.AppendLine();
            report.AppendLine($"TRANSLATION PROGRESS:");
            report.AppendLine($"  ✅ Translated:        {comparison.TranslatedCount,5}");
            report.AppendLine($"  ❌ Remaining:         {comparison.RemainingCount,5}");
            report.AppendLine();
            
            // Detailed changes
            if (comparison.NewCount > 0)
            {
                report.AppendLine("NEW ENTRIES:");
                foreach (var entry in comparison.Entries.Where(e => e.ChangeType == ChangeType.New).Take(20))
                {
                    report.AppendLine($"  • {entry.Key}: \"{entry.NewSourceText}\"");
                }
                if (comparison.NewCount > 20)
                    report.AppendLine($"  ... and {comparison.NewCount - 20} more");
                report.AppendLine();
            }

            if (comparison.ModifiedCount > 0)
            {
                report.AppendLine("MODIFIED ENTRIES:");
                foreach (var entry in comparison.Entries.Where(e => e.ChangeType == ChangeType.Modified).Take(20))
                {
                    report.AppendLine($"  • {entry.Key}:");
                    report.AppendLine($"    OLD: \"{entry.OldSourceText}\"");
                    report.AppendLine($"    NEW: \"{entry.NewSourceText}\"");
                }
                if (comparison.ModifiedCount > 20)
                    report.AppendLine($"  ... and {comparison.ModifiedCount - 20} more");
                report.AppendLine();
            }

            if (comparison.DeletedCount > 0)
            {
                report.AppendLine("DELETED ENTRIES:");
                foreach (var entry in comparison.Entries.Where(e => e.ChangeType == ChangeType.Deleted).Take(20))
                {
                    report.AppendLine($"  • {entry.Key}: \"{entry.OldSourceText}\"");
                }
                if (comparison.DeletedCount > 20)
                    report.AppendLine($"  ... and {comparison.DeletedCount - 20} more");
                report.AppendLine();
            }

            report.AppendLine("=================================================");
            
            return report.ToString();
        }

        /// <summary>
        /// Filter comparison entries by change type
        /// </summary>
        public static List<ComparisonEntry> FilterByChangeType(
            ComparisonResult comparison, 
            ChangeType changeType)
        {
            return comparison.Entries.Where(e => e.ChangeType == changeType).ToList();
        }

        /// <summary>
        /// Get entries that need attention (new or modified)
        /// </summary>
        public static List<ComparisonEntry> GetEntriesNeedingReview(ComparisonResult comparison)
        {
            return comparison.Entries
                .Where(e => e.NeedsReview)
                .OrderBy(e => e.ChangeType) // New first, then Modified
                .ThenBy(e => e.Key)
                .ToList();
        }

        /// <summary>
        /// Batch update translations for selected entries
        /// </summary>
        public static void BatchUpdateTranslations(
            List<ComparisonEntry> entries, 
            Func<string, string> translationFunc)
        {
            foreach (var entry in entries.Where(e => e.IsSelected && e.NeedsReview))
            {
                if (string.IsNullOrWhiteSpace(entry.UpdatedTranslation))
                {
                    var sourceText = entry.NewSourceText;
                    if (!string.IsNullOrWhiteSpace(sourceText))
                    {
                        entry.UpdatedTranslation = translationFunc(sourceText);
                    }
                }
            }
        }
    }
}
