using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ParadoxTranslator.Models
{
    /// <summary>
    /// Represents a specific version of a localization file from a game update
    /// Used for tracking changes across game versions
    /// </summary>
    public class FileVersion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FilePath { get; set; } = string.Empty;
        public string GameVersion { get; set; } = string.Empty; // e.g., "1.0", "1.1.2"
        public DateTime ImportDate { get; set; } = DateTime.Now;
        public List<LocalizationEntry> Entries { get; set; } = new();
        public string FileHash { get; set; } = string.Empty; // MD5 hash to detect changes
        public long FileSize { get; set; }
        public string Notes { get; set; } = string.Empty; // User notes about this version
    }

    /// <summary>
    /// Represents a comparison between two versions of a file entry
    /// </summary>
    public class ComparisonEntry : INotifyPropertyChanged
    {
        private string _key = string.Empty;
        private string _existingTranslation = string.Empty;
        private string _updatedTranslation = string.Empty;
        private bool _isSelected;
        private string _statusMessage = string.Empty;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(nameof(Key)); }
        }
        
        // Old version (before game update)
        public LocalizationEntry? OldEntry { get; set; }
        
        // New version (after game update)
        public LocalizationEntry? NewEntry { get; set; }
        
        // Existing translation from mod
        public string ExistingTranslation
        {
            get => _existingTranslation;
            set { _existingTranslation = value; OnPropertyChanged(nameof(ExistingTranslation)); }
        }
        
        // Updated translation (editable by user)
        public string UpdatedTranslation
        {
            get => _updatedTranslation;
            set { _updatedTranslation = value; OnPropertyChanged(nameof(UpdatedTranslation)); }
        }
        
        // Type of change detected
        public ChangeType ChangeType { get; set; }
        
        // Helper properties for UI
        public string OldSourceText => OldEntry?.SourceText ?? string.Empty;
        public string NewSourceText => NewEntry?.SourceText ?? string.Empty;
        public bool HasSourceTextChanged => OldSourceText != NewSourceText;
        public bool HasTranslation => !string.IsNullOrWhiteSpace(ExistingTranslation);
        public bool NeedsReview => ChangeType == ChangeType.Modified || ChangeType == ChangeType.New;
        
        // For UI binding
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Type of change detected when comparing file versions
    /// </summary>
    public enum ChangeType
    {
        /// <summary>
        /// New key added in the new version (needs translation)
        /// Color: Green 🟢
        /// </summary>
        New,
        
        /// <summary>
        /// Key exists in both but source text changed (review translation)
        /// Color: Yellow 🟡
        /// </summary>
        Modified,
        
        /// <summary>
        /// Key removed in the new version (remove from mod)
        /// Color: Red 🔴
        /// </summary>
        Deleted,
        
        /// <summary>
        /// Key unchanged between versions (keep existing translation)
        /// Color: White ⚪
        /// </summary>
        Unchanged
    }

    /// <summary>
    /// Result of comparing two file versions
    /// </summary>
    public class ComparisonResult
    {
        public FileVersion OldVersion { get; set; } = new();
        public FileVersion NewVersion { get; set; } = new();
        public List<ComparisonEntry> Entries { get; set; } = new();
        public DateTime ComparisonDate { get; set; } = DateTime.Now;
        
        // Statistics
        public int NewCount => Entries.Count(e => e.ChangeType == ChangeType.New);
        public int ModifiedCount => Entries.Count(e => e.ChangeType == ChangeType.Modified);
        public int DeletedCount => Entries.Count(e => e.ChangeType == ChangeType.Deleted);
        public int UnchangedCount => Entries.Count(e => e.ChangeType == ChangeType.Unchanged);
        public int TotalCount => Entries.Count;
        
        // Progress tracking
        public int TranslatedCount => Entries.Count(e => !string.IsNullOrWhiteSpace(e.UpdatedTranslation));
        public int RemainingCount => Entries.Count(e => e.NeedsReview && string.IsNullOrWhiteSpace(e.UpdatedTranslation));
    }
}
