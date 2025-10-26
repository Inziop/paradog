using System.Collections.Generic;

namespace ParadoxTranslator.Models;

public class ScanResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public string? CorrespondingSourceFile { get; set; }
}

public class FolderScanSummary
{
    public string ModFolderPath { get; set; } = string.Empty;
    public int TotalFilesScanned { get; set; }
    public int SourceLanguageFiles { get; set; }
    public Dictionary<string, int> ExistingFilesByLanguage { get; set; } = new();
    public Dictionary<string, int> MissingFilesByLanguage { get; set; } = new();
    public List<ScanResult> AllResults { get; set; } = new();
}
