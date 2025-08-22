using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CopyPastaNative.Models;
using CopyPastaNative.Services;
using MaterialDesignThemes.Wpf;

namespace CopyPastaNative
{
    public partial class MainWindow : Window
    {
        private readonly SnippetService _snippetService;
        private List<Snippet> _allSnippets = new();
        private List<Snippet> _filteredSnippets = new();
        private List<string> _selectedTags = new();
        private bool _isDarkTheme = false;

        public MainWindow()
        {
            InitializeComponent();
            _snippetService = new SnippetService();
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSnippetsAsync();
            UpdateTagsFilter();
            UpdateSnippetsDisplay();
        }

        private async Task LoadSnippetsAsync()
        {
            try
            {
                _allSnippets = await _snippetService.GetAllSnippetsAsync();
                _filteredSnippets = _allSnippets.ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading snippets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTagsFilter()
        {
            TagsFilterPanel.Children.Clear();
            var allTags = _snippetService.GetAllTags();

            foreach (var tag in allTags)
            {
                var button = new Button
                {
                    Content = tag,
                    Style = FindResource("MaterialDesignOutlinedButton") as Style,
                    Margin = new Thickness(0, 0, 8, 8),
                    Tag = tag
                };

                if (_selectedTags.Contains(tag))
                {
                    button.Background = FindResource("MaterialDesignSelection") as Brush;
                    button.Foreground = FindResource("MaterialDesignSelectionForeground") as Brush;
                }

                button.Click += TagFilterButton_Click;
                TagsFilterPanel.Children.Add(button);
            }
        }

        private void UpdateSnippetsDisplay()
        {
            if (_filteredSnippets.Count == 0)
            {
                SnippetsItemsControl.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
            }
            else
            {
                SnippetsItemsControl.Visibility = Visibility.Visible;
                EmptyStatePanel.Visibility = Visibility.Collapsed;
                SnippetsItemsControl.ItemsSource = _filteredSnippets;
            }
        }

        private void ApplyFilters()
        {
            _filteredSnippets = _allSnippets.ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                var searchTerm = SearchBox.Text.ToLowerInvariant();
                _filteredSnippets = _filteredSnippets.Where(s =>
                    s.Title.ToLowerInvariant().Contains(searchTerm) ||
                    s.Code.ToLowerInvariant().Contains(searchTerm) ||
                    s.Tags.Any(tag => tag.ToLowerInvariant().Contains(searchTerm))
                ).ToList();
            }

            // Apply tag filter
            if (_selectedTags.Count > 0)
            {
                _filteredSnippets = _filteredSnippets.Where(s =>
                    _selectedTags.All(tag => s.Tags.Contains(tag))
                ).ToList();
            }

            UpdateSnippetsDisplay();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(300); // Debounce search
            ApplyFilters();
        }

        private void TagFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tag)
            {
                if (_selectedTags.Contains(tag))
                {
                    _selectedTags.Remove(tag);
                    button.Background = FindResource("MaterialDesignPaper") as Brush;
                    button.Foreground = FindResource("MaterialDesignBody") as Brush;
                }
                else
                {
                    _selectedTags.Add(tag);
                    button.Background = FindResource("MaterialDesignSelection") as Brush;
                    button.Foreground = FindResource("MaterialDesignSelectionForeground") as Brush;
                }

                ApplyFilters();
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            _selectedTags.Clear();
            UpdateTagsFilter();
            ApplyFilters();
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            
            // For now, just update the icon since theme switching requires additional setup
            if (_isDarkTheme)
            {
                ThemeToggleButton.Content = new PackIcon { Kind = PackIconKind.WeatherSunny, Width = 24, Height = 24 };
            }
            else
            {
                ThemeToggleButton.Content = new PackIcon { Kind = PackIconKind.WeatherNight, Width = 24, Height = 24 };
            }
        }

        private void NewSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            var snippetDialog = new SnippetDialog();
            if (snippetDialog.ShowDialog() == true)
            {
                var newSnippet = snippetDialog.Snippet;
                _ = Task.Run(async () =>
                {
                    await _snippetService.AddSnippetAsync(newSnippet);
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadSnippetsAsync();
                        UpdateTagsFilter();
                        ApplyFilters();
                    });
                });
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Snippet snippet)
            {
                var snippetDialog = new SnippetDialog(snippet);
                if (snippetDialog.ShowDialog() == true)
                {
                    var updatedSnippet = snippetDialog.Snippet;
                    _ = Task.Run(async () =>
                    {
                        await _snippetService.UpdateSnippetAsync(updatedSnippet);
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            await LoadSnippetsAsync();
                            UpdateTagsFilter();
                            ApplyFilters();
                        });
                    });
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Snippet snippet)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{snippet.Title}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _ = Task.Run(async () =>
                    {
                        await _snippetService.DeleteSnippetAsync(snippet.Id);
                        await Dispatcher.InvokeAsync(async () =>
                        {
                            await LoadSnippetsAsync();
                            UpdateTagsFilter();
                            ApplyFilters();
                        });
                    });
                }
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Snippet snippet)
            {
                try
                {
                    Clipboard.SetText(snippet.Code);
                    
                    // Show success feedback using the named Snackbar
                    if (MainSnackbar != null)
                    {
                        MainSnackbar.MessageQueue?.Enqueue("Code copied to clipboard!");
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't show a dialog since copy is working
                    System.Diagnostics.Debug.WriteLine($"Copy operation note: {ex.Message}");
                }
            }
        }
    }
}
