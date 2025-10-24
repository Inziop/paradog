using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ParadoxTranslator.Services
{
    /// <summary>
    /// Simple local translation memory backed by a JSON file in AppData.
    /// Key is composed from source text + target language to avoid cross-language collisions.
    /// </summary>
    public class TranslationMemoryService
    {
        private readonly string _path;
        private readonly Dictionary<string, string> _memory = new(StringComparer.Ordinal);

        public TranslationMemoryService()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ParadoxTranslator");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "memory.json");
            Load();
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
            catch
            {
                // ignore errors — memory is best-effort
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
            catch
            {
                // ignore save errors for now
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
            return _memory.TryGetValue(key, out target);
        }

        public void SaveMapping(string source, string targetLang, string translated)
        {
            if (string.IsNullOrEmpty(source)) return;
            var key = MakeKey(source, targetLang);
            _memory[key] = translated ?? string.Empty;
            Save();
        }
    }
}
