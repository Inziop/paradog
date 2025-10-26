using System;
using System.Windows;
using System.Windows.Controls;
using ParadoxTranslator.Models;
using MessageBox = ParadoxTranslator.Utils.ModernMessageBox;

namespace ParadoxTranslator
{
    public partial class NewProjectDialog : Window
    {
        public Project? CreatedProject { get; private set; }

        public NewProjectDialog()
        {
            InitializeComponent();
        }

        private void OnCreate(object sender, RoutedEventArgs e)
        {
            var projectName = ProjectNameTextBox.Text.Trim();
            
            if (string.IsNullOrWhiteSpace(projectName))
            {
                MessageBox.Show("Please enter a project name.", "Validation Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                ProjectNameTextBox.Focus();
                return;
            }

            var gameTypeStr = (GameTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            var gameType = string.IsNullOrEmpty(gameTypeStr) || !Enum.TryParse<GameType>(gameTypeStr, out var parsedType)
                ? GameType.Generic
                : parsedType;
            
            var sourceLanguage = (SourceLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";
            var targetLanguage = (TargetLanguageComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "vi";

            CreatedProject = new Project
            {
                Name = projectName,
                Description = DescriptionTextBox.Text.Trim(),
                GameType = gameType,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage
            };

            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
