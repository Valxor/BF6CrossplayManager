using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace BF6CrossplayManager
{
    public partial class MainWindow : Window
    {
        private string profileFilePath = string.Empty;
        private const string CrossplaySettingKey = "GstGameplay.CrossPlayEnable";
        private const string GameProcessName = "bf6";

        public MainWindow()
        {
            InitializeComponent();
            InitializeProfilePath();
            LoadCurrentSettings();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void InitializeProfilePath()
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string bf6SettingsPath = Path.Combine(documentsPath, "Battlefield 6", "settings");
                profileFilePath = Path.Combine(bf6SettingsPath, "PROFSAVE_profile");

                if (File.Exists(profileFilePath))
                {
                    PathLabel.Text = $"File found: {profileFilePath}";
                    PathLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                    EnableButton.IsEnabled = true;
                    DisableButton.IsEnabled = true;
                }
                else
                {
                    PathLabel.Text = $"File not found: {profileFilePath}";
                    PathLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                    StatusLabel.Text = "PROFSAVE_profile file does not exist.\nMake sure Battlefield 6 is installed and you have launched the game at least once.";
                    StatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
                    EnableButton.IsEnabled = false;
                    DisableButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                PathLabel.Text = "Error searching for file";
                PathLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                StatusLabel.Text = $"Error: {ex.Message}";
                StatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
        }

        private void LoadCurrentSettings()
        {
            if (!File.Exists(profileFilePath))
                return;

            try
            {
                string[] lines = File.ReadAllLines(profileFilePath);
                bool found = false;

                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith(CrossplaySettingKey))
                    {
                        found = true;
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            string value = parts[1];
                            if (value == "1")
                            {
                                CurrentStatusLabel.Text = "ENABLED";
                                CurrentStatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                                UpdateButtonStates(true);
                            }
                            else if (value == "0")
                            {
                                CurrentStatusLabel.Text = "DISABLED";
                                CurrentStatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                                UpdateButtonStates(false);
                            }
                        }
                        break;
                    }
                }

                if (!found)
                {
                    CurrentStatusLabel.Text = "NOT CONFIGURED";
                    CurrentStatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error reading file: {ex.Message}";
                StatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
        }

        private void UpdateButtonStates(bool isEnabled)
        {
            if (isEnabled)
            {
                EnableButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
                EnableButton.Foreground = new SolidColorBrush(Colors.White);
                DisableButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                DisableButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
            }
            else
            {
                DisableButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                DisableButton.Foreground = new SolidColorBrush(Colors.White);
                EnableButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                EnableButton.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
            }
        }

        private bool IsBF6Running()
        {
            try
            {
                // Check if BF6 process is running
                Process[] processes = Process.GetProcessesByName(GameProcessName);
                return processes.Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void SaveCrossplaySetting(string value)
        {
            if (!File.Exists(profileFilePath))
            {
                StatusLabel.Text = "❌ Profile file does not exist.";
                StatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
                return;
            }
            if (IsBF6Running())
            {
                MessageBox.Show(
                    "Battlefield 6 is currently running.\n\n" +
                    "Please close the game before modifying crossplay settings.\n\n" +
                    "Changes will not be saved while the game is open.",
                    "Game Running",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                StatusLabel.Text = "⚠️ Save cancelled: Battlefield 6 is currently running.";
                StatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22"));
                return;
            }

            try
            {
                string newLine = $"{CrossplaySettingKey} {value}";
                string[] lines = File.ReadAllLines(profileFilePath);
                bool settingExists = false;
                StringBuilder newContent = new StringBuilder();

                foreach (string line in lines)
                {
                    if (line.Trim().StartsWith(CrossplaySettingKey))
                    {
                        newContent.AppendLine(newLine);
                        settingExists = true;
                    }
                    else
                    {
                        newContent.AppendLine(line);
                    }
                }

                if (!settingExists)
                {
                    newContent.AppendLine(newLine);
                }

                File.WriteAllText(profileFilePath, newContent.ToString());

                string statusText = value == "1" ? "ENABLED" : "DISABLED";
                StatusLabel.Text = $"✓ Crossplay {statusText} successfully!";
                StatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));

                LoadCurrentSettings();
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"❌ Error saving: {ex.Message}";
                StatusLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            }
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCrossplaySetting("1");
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCrossplaySetting("0");
        }
    }
}
