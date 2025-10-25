using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services
{
    public static class ProjectService
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ParadoxTranslator");
        
        private static readonly string ProjectsFolder = Path.Combine(AppDataFolder, "Projects");
        private static readonly string ProjectsListFile = Path.Combine(AppDataFolder, "projects.json");

        static ProjectService()
        {
            // Ensure directories exist
            Directory.CreateDirectory(ProjectsFolder);
        }

        public static List<Project> GetAllProjects()
        {
            try
            {
                if (!File.Exists(ProjectsListFile))
                    return new List<Project>();

                var json = File.ReadAllText(ProjectsListFile);
                var projects = JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
                
                // Sort by last opened date (most recent first)
                return projects.OrderByDescending(p => p.LastOpenedDate).ToList();
            }
            catch
            {
                return new List<Project>();
            }
        }

        public static void SaveProject(Project project)
        {
            try
            {
                // Update last opened date
                project.LastOpenedDate = DateTime.Now;

                // Get all existing projects
                var projects = GetAllProjects();
                
                // Update or add the project
                var existingIndex = projects.FindIndex(p => p.Id == project.Id);
                if (existingIndex >= 0)
                    projects[existingIndex] = project;
                else
                    projects.Add(project);

                // Save to file
                var json = JsonSerializer.Serialize(projects, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(ProjectsListFile, json);

                // Also save individual project file
                var projectFile = Path.Combine(ProjectsFolder, $"{project.Id}.json");
                var projectJson = JsonSerializer.Serialize(project, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(projectFile, projectJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save project: {ex.Message}");
            }
        }

        public static Project? LoadProject(string projectId)
        {
            try
            {
                var projectFile = Path.Combine(ProjectsFolder, $"{projectId}.json");
                if (!File.Exists(projectFile))
                    return null;

                var json = File.ReadAllText(projectFile);
                return JsonSerializer.Deserialize<Project>(json);
            }
            catch
            {
                return null;
            }
        }

        public static void DeleteProject(string projectId)
        {
            try
            {
                // Remove from projects list
                var projects = GetAllProjects();
                projects.RemoveAll(p => p.Id == projectId);
                
                var json = JsonSerializer.Serialize(projects, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(ProjectsListFile, json);

                // Delete individual project file
                var projectFile = Path.Combine(ProjectsFolder, $"{projectId}.json");
                if (File.Exists(projectFile))
                    File.Delete(projectFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete project: {ex.Message}");
            }
        }

        public static List<Project> GetRecentProjects(int count = 5)
        {
            return GetAllProjects().Take(count).ToList();
        }
    }
}
