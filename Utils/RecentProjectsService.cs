using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ParadoxTranslator.Utils
{
    public class RecentProject
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime LastOpened { get; set; }
    }

    public static class RecentProjectsService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ParadoxTranslator");
        private static readonly string RecentFile = Path.Combine(AppDataFolder, "recent.json");

        public static List<RecentProject> Load()
        {
            try
            {
                if (!File.Exists(RecentFile)) return new List<RecentProject>();
                var json = File.ReadAllText(RecentFile);
                return JsonSerializer.Deserialize<List<RecentProject>>(json) ?? new List<RecentProject>();
            }
            catch
            {
                return new List<RecentProject>();
            }
        }

        public static void Save(List<RecentProject> projects)
        {
            try
            {
                if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);
                var json = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(RecentFile, json);
            }
            catch { }
        }

        public static void AddOrUpdate(string name, string path)
        {
            var projects = Load();
            var existing = projects.Find(p => p.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Name = name;
                existing.LastOpened = DateTime.Now;
            }
            else
            {
                projects.Insert(0, new RecentProject { Name = name, Path = path, LastOpened = DateTime.Now });
            }
            // Keep max 10 recent
            if (projects.Count > 10) projects.RemoveRange(10, projects.Count - 10);
            Save(projects);
        }
    }
}
