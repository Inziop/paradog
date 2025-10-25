using System.IO;
using System.Text;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

/// <summary>
/// Service for file operations
/// </summary>
public class FileService
{
    /// <summary>
    /// Get all localization files in a directory
    /// </summary>
    public static Task<List<string>> GetLocalizationFilesAsync(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return Task.FromResult(new List<string>());

        var files = new List<string>();
        var searchPatterns = new[] { "*.yml", "*.yaml", "*.txt" };

        foreach (var pattern in searchPatterns)
        {
            files.AddRange(Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories));
        }

        return Task.FromResult(files.OrderBy(f => f).ToList());
    }

    /// <summary>
    /// Detect file encoding
    /// </summary>
    public static Encoding DetectEncoding(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        
        // Read a small portion to detect encoding
        var buffer = new char[1024];
        reader.Read(buffer, 0, buffer.Length);
        
        return reader.CurrentEncoding;
    }

    /// <summary>
    /// Create backup of file
    /// </summary>
    public static Task<string> CreateBackupAsync(string filePath)
    {
        var backupDir = Path.Combine(Path.GetDirectoryName(filePath)!, "backup");
        Directory.CreateDirectory(backupDir);

        var fileName = Path.GetFileName(filePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(backupDir, $"{timestamp}_{fileName}");

        File.Copy(filePath, backupPath);
        return Task.FromResult(backupPath);
    }

    /// <summary>
    /// Save file with UTF-8 encoding
    /// </summary>
    public static async Task SaveFileAsync(string filePath, string content, bool includeBom = false, bool createBackup = true)
    {
        // Create backup if enabled in settings
        if (createBackup && File.Exists(filePath))
        {
            var config = SettingsService.LoadConfig();
            if (config.CreateBackupBeforeSave)
            {
                try
                {
                    await CreateBackupAsync(filePath);
                }
                catch (Exception ex)
                {
                    // Continue even if backup fails, but log the error
                    LoggingService.Error($"Failed to create backup for {filePath}", ex);
                }
            }
        }

        var encoding = includeBom ? new UTF8Encoding(true) : new UTF8Encoding(false);
        await File.WriteAllTextAsync(filePath, content, encoding);
    }

    /// <summary>
    /// Get file size in human readable format
    /// </summary>
    public static string GetFileSizeString(string filePath)
    {
        if (!File.Exists(filePath))
            return "0 B";

        var bytes = new FileInfo(filePath).Length;
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Get file modification time
    /// </summary>
    public static DateTime GetFileModifiedTime(string filePath)
    {
        return File.GetLastWriteTime(filePath);
    }
}
