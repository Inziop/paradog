using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ParadoxTranslator.Services
{
    /// <summary>
    /// Simple local translation memory backed by a JSON file in AppData.
    /// Key is composed from source text + target language to avoid cross-language collisions.
    /// Implements LRU (Least Recently Used) cache with configurable size limit.
    /// </summary>
    public class TranslationMemoryService
    {
        private static readonly Lazy<TranslationMemoryService> _instance = new(() => new TranslationMemoryService());
        public static TranslationMemoryService Instance => _instance.Value;

        private readonly string _path;
        private readonly Dictionary<string, string> _memory = new(StringComparer.Ordinal);
        private readonly LinkedList<string> _lruList = new(); // Track access order
        private readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new(StringComparer.Ordinal);
        private int _maxEntries = 100000; // Default limit

        private TranslationMemoryService()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParadoxTranslator");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "memory.json");
            LoadMaxEntriesFromConfig();
            Load();
        }

        private void LoadMaxEntriesFromConfig()
        {
            try
            {
                var config = SettingsService.LoadConfig();
                var newLimit = Math.Clamp(config.TranslationMemoryLimit, 20000, 1500000); // Min 20k, Max 1.5M
                
                // If limit decreased and we have more entries than new limit, enforce immediately
                if (newLimit < _maxEntries && _memory.Count > newLimit)
                {
                    _maxEntries = newLimit;
                    EnforceSizeLimit();
                    Save(); // Persist the trimmed memory
                }
                else
                {
                    _maxEntries = newLimit;
                }
            }
            catch
            {
                _maxEntries = 100000; // Fallback to default
            }
        }

        /// <summary>
        /// Reload config and enforce new limit (call this after settings changed)
        /// </summary>
        public void ReloadConfig()
        {
            LoadMaxEntriesFromConfig();
        }

        private void UpdateLRU(string key)
        {
            // Remove from current position if exists
            if (_lruNodes.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
            }

            // Add to front (most recently used)
            var newNode = _lruList.AddFirst(key);
            _lruNodes[key] = newNode;
        }

        private void EnforceSizeLimit()
        {
            while (_memory.Count > _maxEntries && _lruList.Last != null)
            {
                // Remove least recently used (from tail)
                var oldestKey = _lruList.Last.Value;
                _lruList.RemoveLast();
                _lruNodes.Remove(oldestKey);
                _memory.Remove(oldestKey);
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path))
                    return;

                var json = File.ReadAllText(_path);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);
                if (dict != null)
                {
                    foreach (var kv in dict)
                        _memory[kv.Key] = kv.Value;
                }
            }
            catch (Exception ex)
            {
                // Ignore errors — memory is best-effort, but log for debugging
                LoggingService.Error("Failed to load translation memory", ex);
            }
        }

        private void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_memory, options);
                File.WriteAllText(_path, json);
            }
            catch (Exception ex)
            {
                // Ignore save errors for now, but log for debugging
                LoggingService.Error("Failed to save translation memory", ex);
            }
        }

        private string MakeKey(string source, string targetLang)
        {
            return source + "|" + (targetLang ?? string.Empty);
        }

        public bool TryLookup(string source, string targetLang, out string target)
        {
            target = string.Empty;
            if (string.IsNullOrEmpty(source)) return false;
            var key = MakeKey(source, targetLang);
            
            if (_memory.TryGetValue(key, out target!)) // ! because we initialize target to empty string above
            {
                UpdateLRU(key); // Mark as recently used
                return true;
            }
            return false;
        }

        public void SaveMapping(string source, string targetLang, string translated)
        {
            if (string.IsNullOrEmpty(source)) return;
            var key = MakeKey(source, targetLang);
            _memory[key] = translated ?? string.Empty;
            UpdateLRU(key); // Mark as recently used
            EnforceSizeLimit(); // Check if we need to evict old entries
            Save();
        }
    }
}
