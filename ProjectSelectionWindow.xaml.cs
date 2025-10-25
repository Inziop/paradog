using System.Windows;
using System.Windows.Controls;
using ParadoxTranslator.Models;
using ParadoxTranslator.Services;

namespace ParadoxTranslator
{
    public partial class ProjectSelectionWindow : Window
    {
        public Project? SelectedProject { get; private set; }

        public ProjectSelectionWindow()
        {
            InitializeComponent();
            LoadRecentProjects();
        }

        private void LoadRecentProjects()
        {
            var recentProjects = ProjectService.GetRecentProjects(10);
            
            if (recentProjects.Count > 0)
            {
                RecentProjectsList.ItemsSource = recentProjects;
                EmptyState.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyState.Visibility = Visibility.Visible;
            }
        }

        private void OnNewProject(object sender, RoutedEventArgs e)
        {
            var dialog = new NewProjectDialog();
            var result = dialog.ShowDialog();
            
            System.Diagnostics.Debug.WriteLine($"NewProjectDialog result: {result}");
            System.Diagnostics.Debug.WriteLine($"CreatedProject: {dialog.CreatedProject?.Name ?? "null"}");
            
            if (result == true && dialog.CreatedProject != null)
            {
                SelectedProject = dialog.CreatedProject;
                ProjectService.SaveProject(SelectedProject);
                System.Diagnostics.Debug.WriteLine($"Setting DialogResult = true");
                DialogResult = true;
                Close();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Dialog was cancelled or project is null");
            }
        }

        private void OnOpenProject(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open Project File",
                Filter = "Project Files (*.ptproj)|*.ptproj|All Files (*.*)|*.*",
                DefaultExt = ".ptproj"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // TODO: Implement custom project file format if needed
                // For now, users can select from recent projects
                MessageBox.Show("Please select from recent projects or create a new one.", 
                               "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnOpenRecentProject(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string projectId)
            {
                var project = ProjectService.LoadProject(projectId);
                if (project != null)
                {
                    SelectedProject = project;
                    ProjectService.SaveProject(project); // Update last opened date
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Failed to load project.", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnMinimize(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
