using System;
using System.Windows;
using System.Windows.Controls;

namespace SKO.GridPCULimiter
{
    public partial class GridPCULimiterControl : UserControl
    {
        private SKOGridPCULimiterPlugin Plugin { get; }

        private GridPCULimiterControl()
        {
            InitializeComponent();
        }

        public GridPCULimiterControl(SKOGridPCULimiterPlugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Plugin.Save();
                MessageBox.Show("Configuration saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSteamId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a simple input dialog
                var inputDialog = new Window
                {
                    Title = "Add Steam ID",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this)
                };

                var stackPanel = new StackPanel { Margin = new Thickness(10) };

                var label = new TextBlock
                {
                    Text = "Enter Steam ID (64-bit number):",
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var textBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var okButton = new Button
                {
                    Content = "OK",
                    Width = 75,
                    Margin = new Thickness(0, 0, 10, 0),
                    IsDefault = true
                };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    Width = 75,
                    IsCancel = true
                };

                bool? dialogResult = null;
                okButton.Click += (s, args) =>
                {
                    dialogResult = true;
                    inputDialog.Close();
                };

                cancelButton.Click += (s, args) =>
                {
                    dialogResult = false;
                    inputDialog.Close();
                };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);

                stackPanel.Children.Add(label);
                stackPanel.Children.Add(textBox);
                stackPanel.Children.Add(buttonPanel);

                inputDialog.Content = stackPanel;
                inputDialog.ShowDialog();

                if (dialogResult == true && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    if (ulong.TryParse(textBox.Text, out ulong steamId))
                    {
                        if (!Plugin.Config.ExemptSteamIds.Contains(steamId))
                        {
                            Plugin.Config.ExemptSteamIds.Add(steamId);
                            MessageBox.Show("Steam ID added successfully!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("This Steam ID is already in the exempt list.", "Duplicate",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid Steam ID format. Please enter a valid 64-bit number.", "Invalid Input",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding Steam ID: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSteamId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.DataContext is ulong steamId)
                {
                    var result = MessageBox.Show($"Are you sure you want to remove Steam ID {steamId} from the exempt list?",
                        "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Plugin.Config.ExemptSteamIds.Remove(steamId);
                        MessageBox.Show("Steam ID removed successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing Steam ID: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
