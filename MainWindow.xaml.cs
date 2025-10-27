using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using CopyPastaNative.Models;
using CopyPastaNative.Services;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;

namespace CopyPastaNative
{
    public partial class MainWindow : Window
    {
        private readonly SnippetService _snippetService;
        private readonly PaletteHelper _paletteHelper;
        private List<Snippet> _allSnippets = new();
        private List<Snippet> _filteredSnippets = new();
        private List<string> _selectedTags = new();
        private bool _isDarkTheme = false;
        private bool _showFavoritesOnly = false;
        private bool _isMultiSelectMode = false;
        private List<string> _searchHistory = new(); // Stores up to 10 recent searches

        public MainWindow()
        {
            InitializeComponent();
            
            _snippetService = new SnippetService();
            _selectedTags = new List<string>();
            _paletteHelper = new PaletteHelper();
            
            // Initialize theme toggle button
            ThemeToggleButton.Content = "🌙";
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSnippetsAsync();
            UpdateTagsFilter();
            UpdateSnippetsDisplayDirect();
            
            // CRITICAL: Apply dark theme on startup if enabled
            if (_isDarkTheme)
            {
                System.Diagnostics.Debug.WriteLine("Applying dark theme on startup");
                UpdateSnippetElementsTheme(true);
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Only handle keyboard shortcuts when not typing in a text box
                if (e.OriginalSource is TextBox || e.OriginalSource is ICSharpCode.AvalonEdit.TextEditor)
                {
                    return;
                }

                var key = e.Key;
                var modifiers = Keyboard.Modifiers;

                // Ctrl+F - Focus search box
                if (key == Key.F && modifiers == ModifierKeys.Control)
                {
                    SearchBox?.Focus();
                    SearchBox?.SelectAll();
                    e.Handled = true;
                    System.Diagnostics.Debug.WriteLine("Keyboard shortcut: Ctrl+F - Focused search box");
                    return;
                }

                // Ctrl+N - New snippet
                if (key == Key.N && modifiers == ModifierKeys.Control)
                {
                    NewSnippetButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    System.Diagnostics.Debug.WriteLine("Keyboard shortcut: Ctrl+N - New snippet");
                    return;
                }

                // Escape - Clear filters
                if (key == Key.Escape && modifiers == ModifierKeys.None)
                {
                    ClearFiltersButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                    System.Diagnostics.Debug.WriteLine("Keyboard shortcut: Escape - Cleared filters");
                    return;
                }

                // Delete - Delete selected snippet
                if (key == Key.Delete && modifiers == ModifierKeys.None)
                {
                    if (SnippetsListView?.SelectedItem is Snippet selectedSnippet)
                    {
                        var result = MessageBox.Show(
                            $"Are you sure you want to delete '{selectedSnippet.Title}'?",
                            "Confirm Delete",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            _ = Task.Run(async () =>
                            {
                                await _snippetService.DeleteSnippetAsync(selectedSnippet.Id);
                                await Dispatcher.InvokeAsync(async () =>
                                {
                                    await LoadSnippetsAsync();
                                    UpdateTagsFilter();
                                    ApplyNewFilteringSystem();
                                    
                                    if (_isDarkTheme)
                                    {
                                        UpdateSnippetElementsTheme(true);
                                    }
                                });
                            });
                            e.Handled = true;
                            System.Diagnostics.Debug.WriteLine($"Keyboard shortcut: Delete - Deleting snippet '{selectedSnippet.Title}'");
                        }
                    }
                    return;
                }

                // Ctrl+A - Select all (when in multi-select mode)
                if (key == Key.A && modifiers == ModifierKeys.Control)
                {
                    if (_isMultiSelectMode && SnippetsListView != null)
                    {
                        SnippetsListView.SelectAll();
                        UpdateSelectedCount();
                        e.Handled = true;
                        System.Diagnostics.Debug.WriteLine("Keyboard shortcut: Ctrl+A - Selected all items");
                    }
                    return;
                }

                // Ctrl+D - Deselect all (when in multi-select mode)
                if (key == Key.D && modifiers == ModifierKeys.Control)
                {
                    if (_isMultiSelectMode && SnippetsListView != null)
                    {
                        SnippetsListView.SelectedItems.Clear();
                        UpdateSelectedCount();
                        e.Handled = true;
                        System.Diagnostics.Debug.WriteLine("Keyboard shortcut: Ctrl+D - Deselected all items");
                    }
                    return;
                }

                // Ctrl+C - Copy code from selected snippet
                if (key == Key.C && modifiers == ModifierKeys.Control)
                {
                    if (SnippetsListView?.SelectedItem is Snippet selectedSnippet)
                    {
                        try
                        {
                            Clipboard.SetText(selectedSnippet.Code);
                            System.Diagnostics.Debug.WriteLine($"Keyboard shortcut: Ctrl+C - Copied code from '{selectedSnippet.Title}'");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error copying code via keyboard shortcut: {ex.Message}");
                        }
                        e.Handled = true;
                    }
                    return;
                }

                // Enter - Copy and select next item
                if (key == Key.Enter && modifiers == ModifierKeys.None)
                {
                    if (SnippetsListView?.SelectedItem is Snippet selectedSnippet)
                    {
                        try
                        {
                            Clipboard.SetText(selectedSnippet.Code);
                            System.Diagnostics.Debug.WriteLine($"Keyboard shortcut: Enter - Copied code from '{selectedSnippet.Title}'");
                            
                            // Move to next item or first item if at the end
                            var index = SnippetsListView.SelectedIndex;
                            var maxIndex = SnippetsListView.Items.Count - 1;
                            if (index < maxIndex)
                            {
                                SnippetsListView.SelectedIndex = index + 1;
                                SnippetsListView.ScrollIntoView(SnippetsListView.SelectedItem);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error copying code via Enter key: {ex.Message}");
                        }
                        e.Handled = true;
                    }
                    return;
                }

                // Up/Down arrows - Navigate snippets
                if ((key == Key.Up || key == Key.Down) && modifiers == ModifierKeys.None)
                {
                    var currentIndex = SnippetsListView?.SelectedIndex ?? 0;
                    var itemCount = SnippetsListView?.Items.Count ?? 0;
                    
                    if (itemCount > 0)
                    {
                        int newIndex;
                        if (key == Key.Up)
                        {
                            newIndex = currentIndex > 0 ? currentIndex - 1 : itemCount - 1;
                        }
                        else // Key.Down
                        {
                            newIndex = currentIndex < itemCount - 1 ? currentIndex + 1 : 0;
                        }
                        
                        SnippetsListView.SelectedIndex = newIndex;
                        SnippetsListView.ScrollIntoView(SnippetsListView.SelectedItem);
                        e.Handled = true;
                        System.Diagnostics.Debug.WriteLine($"Keyboard shortcut: {key} - Navigated to index {newIndex}");
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling keyboard shortcut: {ex.Message}");
            }
        }

        private async Task LoadSnippetsAsync()
        {
            try
            {
                _allSnippets = await _snippetService.GetAllSnippetsAsync();
                _filteredSnippets = _allSnippets.ToList();
                
                System.Diagnostics.Debug.WriteLine($"LoadSnippetsAsync: Loaded {_allSnippets.Count} snippets");
                foreach (var snippet in _allSnippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
                
                // Update statistics after loading
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading snippets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatistics()
        {
            try
            {
                if (_allSnippets == null || _allSnippets.Count == 0)
                {
                    StatsTotalSnippets.Text = "0";
                    StatsFavoriteSnippets.Text = "0";
                    StatsLanguages.Text = "0";
                    StatsTotalTags.Text = "0";
                    StatsTopTag.Text = "None";
                    return;
                }

                // Total snippets
                StatsTotalSnippets.Text = _allSnippets.Count.ToString();

                // Favorite snippets
                var favoriteCount = _allSnippets.Count(s => s.IsFavorite);
                StatsFavoriteSnippets.Text = favoriteCount.ToString();

                // Unique languages
                var languages = _allSnippets
                    .Where(s => !string.IsNullOrWhiteSpace(s.Language))
                    .Select(s => s.Language)
                    .Distinct()
                    .ToList();
                StatsLanguages.Text = languages.Count.ToString();

                // Total tags count
                var allTags = _allSnippets
                    .Where(s => s.Tags != null)
                    .SelectMany(s => s.Tags)
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .ToList();
                StatsTotalTags.Text = allTags.Distinct().Count().ToString();

                // Most popular tag
                var mostPopularTag = allTags
                    .GroupBy(tag => tag)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (mostPopularTag != null)
                {
                    StatsTopTag.Text = mostPopularTag.Key;
                }
                else
                {
                    StatsTopTag.Text = "None";
                }

                System.Diagnostics.Debug.WriteLine($"Statistics updated: {_allSnippets.Count} total, {favoriteCount} favorites, {languages.Count} languages");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating statistics: {ex.Message}");
            }
        }

        private void UpdateStatisticsPanelTheme(bool isDark)
        {
            try
            {
                // Update Statistics panel border
                if (StatisticsPanel != null)
                {
                    StatisticsPanel.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                    StatisticsPanel.BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(80, 80, 80)) : new SolidColorBrush(Color.FromRgb(224, 224, 224));
                }
                
                // Update Statistics header
                if (StatisticsHeader != null)
                {
                    StatisticsHeader.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                }
                
                // Update all TextBlocks in the statistics panel (including Runs)
                UpdateStatisticsTextBlocks(isDark);
                
                // Update all PackIcons in the statistics panel
                UpdateStatisticsIcons(isDark);
                
                System.Diagnostics.Debug.WriteLine($"Statistics panel theme updated to {(isDark ? "dark" : "light")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating statistics panel theme: {ex.Message}");
            }
        }

        private void UpdateStatisticsTextBlocks(bool isDark)
        {
            try
            {
                // Update all TextBlocks within the Statistics panel
                var statisticsPanel = this.FindName("StatisticsPanel") as Border;
                if (statisticsPanel != null)
                {
                    var textBlocks = FindVisualChildren<TextBlock>(statisticsPanel);
                    foreach (var textBlock in textBlocks)
                    {
                        textBlock.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating statistics text blocks: {ex.Message}");
            }
        }

        private void UpdateStatisticsIcons(bool isDark)
        {
            try
            {
                // Update all PackIcons within the Statistics panel
                var statisticsPanel = this.FindName("StatisticsPanel") as Border;
                if (statisticsPanel != null)
                {
                    var icons = FindVisualChildren<MaterialDesignThemes.Wpf.PackIcon>(statisticsPanel);
                    foreach (var icon in icons)
                    {
                        icon.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating statistics icons: {ex.Message}");
            }
        }

        private void UpdateTagButtonVisualStates()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTagButtonVisualStates: Updating visual states for {_selectedTags.Count} selected tags");
                
                foreach (var child in TagsFilterPanel.Children)
                {
                    if (child is Button button && button.Tag is string tag)
                    {
                        if (_selectedTags.Contains(tag))
                        {
                            button.Background = FindResource("MaterialDesignSelection") as Brush ?? new SolidColorBrush(Colors.Blue);
                            button.Foreground = FindResource("MaterialDesignSelectionForeground") as Brush ?? new SolidColorBrush(Colors.White);
                            System.Diagnostics.Debug.WriteLine($"  Tag '{tag}' button set to SELECTED state");
                        }
                        else
                        {
                            button.Background = FindResource("MaterialDesignPaper") as Brush ?? new SolidColorBrush(Colors.White);
                            button.Foreground = FindResource("MaterialDesignBody") as Brush ?? new SolidColorBrush(Colors.Black);
                            System.Diagnostics.Debug.WriteLine($"  Tag '{tag}' button set to UNSELECTED state");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("UpdateTagButtonVisualStates: Completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateTagButtonVisualStates: {ex.Message}");
            }
        }

        private void UpdateTagsFilter()
        {
            try
            {
                TagsFilterPanel.Children.Clear();
                
                // Safety check - ensure snippets are loaded
                if (_allSnippets == null || _allSnippets.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No snippets loaded, skipping tag filter update");
                    return;
                }
                
                var allTags = _snippetService.GetAllTags();
                System.Diagnostics.Debug.WriteLine($"Found {allTags.Count} tags: {string.Join(", ", allTags)}");

                foreach (var tag in allTags)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(tag)) continue; // Skip null/empty tags
                        
                        var button = new Button
                        {
                            Content = tag,
                            Style = FindResource("MaterialDesignOutlinedButton") as Style,
                            Margin = new Thickness(0, 0, 8, 8),
                            Tag = tag
                        };

                        // Set the correct visual state based on selection
                        if (_selectedTags.Contains(tag))
                        {
                            button.Background = FindResource("MaterialDesignSelection") as Brush ?? new SolidColorBrush(Colors.Blue);
                            button.Foreground = FindResource("MaterialDesignSelectionForeground") as Brush ?? new SolidColorBrush(Colors.White);
                        }
                        else
                        {
                            button.Background = FindResource("MaterialDesignPaper") as Brush ?? new SolidColorBrush(Colors.White);
                            button.Foreground = FindResource("MaterialDesignBody") as Brush ?? new SolidColorBrush(Colors.Black);
                        }

                        button.Click += TagFilterButton_Click;
                        TagsFilterPanel.Children.Add(button);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating tag button for '{tag}': {ex.Message}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Created {TagsFilterPanel.Children.Count} tag filter buttons");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateTagsFilter: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void UpdateSnippetsDisplay()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSnippetsDisplay called with {_filteredSnippets?.Count ?? 0} filtered snippets");
                
                if (_filteredSnippets == null || _filteredSnippets.Count == 0)
                {
                    SnippetsListView.Visibility = Visibility.Collapsed;
                    EmptyStatePanel.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("Showing empty state - no snippets to display");
                }
                else
                {
                    SnippetsListView.Visibility = Visibility.Visible;
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                    
                    // Simple and direct approach - just update the ItemsSource
                    SnippetsListView.ItemsSource = null;
                    SnippetsListView.ItemsSource = _filteredSnippets;
                    
                    System.Diagnostics.Debug.WriteLine($"Updated ListView with {_filteredSnippets.Count} snippets");
                    System.Diagnostics.Debug.WriteLine($"ListView.Items.Count: {SnippetsListView.Items.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateSnippetsDisplay: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void UpdateSnippetsDisplayDirect()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSnippetsDisplayDirect called with {_filteredSnippets?.Count ?? 0} filtered snippets");
                System.Diagnostics.Debug.WriteLine($"ListView.Items.Count before update: {SnippetsListView?.Items?.Count ?? 0}");
                
                if (_filteredSnippets == null || _filteredSnippets.Count == 0)
                {
                    SnippetsListView.Visibility = Visibility.Collapsed;
                    EmptyStatePanel.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("Showing empty state - no snippets to display");
                }
                else
                {
                    SnippetsListView.Visibility = Visibility.Visible;
                    EmptyStatePanel.Visibility = Visibility.Collapsed;
                    
                    // COMPLETELY BYPASS BINDING - Direct ListView manipulation
                    System.Diagnostics.Debug.WriteLine($"Setting ListView.ItemsSource to {_filteredSnippets.Count} snippets");
                    SnippetsListView.ItemsSource = null;
                    System.Diagnostics.Debug.WriteLine($"ListView.ItemsSource set to null");
                    SnippetsListView.ItemsSource = _filteredSnippets;
                    System.Diagnostics.Debug.WriteLine($"ListView.ItemsSource set to {_filteredSnippets.Count} snippets");
                    
                    System.Diagnostics.Debug.WriteLine($"DIRECT UPDATE: Set ListView ItemsSource to {_filteredSnippets.Count} snippets");
                    System.Diagnostics.Debug.WriteLine($"DIRECT UPDATE: ListView.Items.Count after setting: {SnippetsListView.Items.Count}");
                    
                    // Force immediate refresh
                    System.Diagnostics.Debug.WriteLine("Forcing ListView refresh...");
                    SnippetsListView.Items.Refresh();
                    System.Diagnostics.Debug.WriteLine("ListView.Items.Refresh() completed");
                    SnippetsListView.InvalidateVisual();
                    System.Diagnostics.Debug.WriteLine("ListView.InvalidateVisual() completed");
                    SnippetsListView.UpdateLayout();
                    System.Diagnostics.Debug.WriteLine("ListView.UpdateLayout() completed");
                    
                    // Force parent container refresh
                    if (SnippetsListView.Parent is FrameworkElement parent)
                    {
                        System.Diagnostics.Debug.WriteLine("Forcing parent container refresh...");
                        parent.InvalidateVisual();
                        parent.UpdateLayout();
                        System.Diagnostics.Debug.WriteLine("Parent container refresh completed");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"DIRECT UPDATE: UI refresh completed");
                    System.Diagnostics.Debug.WriteLine($"Final ListView.Items.Count: {SnippetsListView.Items.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateSnippetsDisplayDirect: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void ApplyFilters()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== APPLYING FILTERS ===");
                System.Diagnostics.Debug.WriteLine($"Selected tags: [{string.Join("][", _selectedTags ?? new List<string>())}]");
                
                // Safety check - ensure we have snippets to filter
                if (_allSnippets == null)
                {
                    _filteredSnippets = new List<Snippet>();
                    UpdateSnippetsDisplayDirect();
                    return;
                }
                
                // Start with all snippets
                var filteredList = new List<Snippet>(_allSnippets);
                System.Diagnostics.Debug.WriteLine($"Starting with {filteredList.Count} snippets:");
                foreach (var snippet in filteredList)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join("][", snippet.Tags ?? new List<string>())}]");
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchBox?.Text))
                {
                    var searchTerm = SearchBox.Text.ToLowerInvariant();
                    filteredList = filteredList.Where(s =>
                        s != null && 
                        (s.Title?.ToLowerInvariant().Contains(searchTerm) == true ||
                         s.Code?.ToLowerInvariant().Contains(searchTerm) == true ||
                         s.Tags?.Any(tag => tag?.ToLowerInvariant().Contains(searchTerm) == true) == true)
                    ).ToList();
                    System.Diagnostics.Debug.WriteLine($"After search filter: {filteredList.Count} snippets");
                }

                // Apply tag filter - show snippets that have ANY of the selected tags
                if (_selectedTags != null && _selectedTags.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Applying tag filter with {_selectedTags.Count} selected tags: {string.Join(", ", _selectedTags)}");
                    
                    var beforeTagFilter = filteredList.Count;
                    
                    // Simple filtering: keep snippets that have ANY of the selected tags
                    filteredList = filteredList.Where(s => 
                    {
                        if (s == null || s.Tags == null) return false;
                        
                        var hasAnyTag = s.Tags.Any(tag => _selectedTags.Contains(tag));
                        System.Diagnostics.Debug.WriteLine($"  Checking snippet '{s.Title}' with tags [{string.Join("][", s.Tags)}] - Has any selected tag: {hasAnyTag}");
                        
                        return hasAnyTag;
                    }).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Tag filter: {beforeTagFilter} -> {filteredList.Count} snippets");
                    
                    // Log which snippets remain after filtering
                    if (filteredList.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Remaining snippets after tag filter:");
                        foreach (var snippet in filteredList)
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {snippet.Title} (Tags: [{string.Join("][", snippet.Tags.Select(t => $"'{t}'"))}])");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("NO SNIPPETS REMAIN AFTER TAG FILTERING!");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No tags selected, showing all snippets");
                }

                System.Diagnostics.Debug.WriteLine($"Final filtered count: {filteredList.Count}");
                
                // Update the filtered snippets list
                _filteredSnippets = filteredList;
                
                // DIRECT UI UPDATE - bypass all binding issues
                UpdateSnippetsDisplayDirect();
                
                System.Diagnostics.Debug.WriteLine("=== FILTERS APPLIED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyFilters: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Fallback - show all snippets if filtering fails
                try
                {
                    _filteredSnippets = _allSnippets?.ToList() ?? new List<Snippet>();
                    UpdateSnippetsDisplayDirect();
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback also failed: {fallbackEx.Message}");
                }
            }
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await Task.Delay(300); // Debounce search
            ApplyNewFilteringSystem();
            
            // Add search to history
            if (SearchBox != null && !string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string searchText = SearchBox.Text.Trim();
                
                // Remove if already exists (to avoid duplicates)
                _searchHistory.Remove(searchText);
                
                // Add to the beginning
                _searchHistory.Insert(0, searchText);
                
                // Keep only last 10 searches
                if (_searchHistory.Count > 10)
                {
                    _searchHistory.RemoveAt(_searchHistory.Count - 1);
                }
                
                // Update the search history UI
                UpdateSearchHistoryUI();
            }
        }

        private void TagFilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string tag)
                {
                    System.Diagnostics.Debug.WriteLine($"=== BRAND NEW FILTERING SYSTEM ===");
                    System.Diagnostics.Debug.WriteLine($"Tag clicked: '{tag}'");
                    System.Diagnostics.Debug.WriteLine($"Button state before: Background={button.Background}, Foreground={button.Foreground}");
                    System.Diagnostics.Debug.WriteLine($"Current _selectedTags count: {_selectedTags?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"Current _allSnippets count: {_allSnippets?.Count ?? 0}");
                    
                    if (string.IsNullOrEmpty(tag)) return;
                    
                    // BRAND NEW APPROACH: Handle selection state first, then filter
                    bool wasSelected = _selectedTags.Contains(tag);
                    System.Diagnostics.Debug.WriteLine($"Tag '{tag}' was previously selected: {wasSelected}");
                    
                    if (wasSelected)
                    {
                        // REMOVE tag first
                        _selectedTags.Remove(tag);
                        System.Diagnostics.Debug.WriteLine($"REMOVED tag: '{tag}' - Tags now: [{string.Join(", ", _selectedTags)}]");
                        
                        // Update button visual state with safe fallbacks
                        button.Background = new SolidColorBrush(Colors.White);
                        button.Foreground = new SolidColorBrush(Colors.Black);
                        System.Diagnostics.Debug.WriteLine($"Button visual state updated to UNSELECTED");
                    }
                    else
                    {
                        // ADD tag first
                        _selectedTags.Add(tag);
                        System.Diagnostics.Debug.WriteLine($"ADDED tag: '{tag}' - Tags now: [{string.Join(", ", _selectedTags)}]");
                        
                        // Update button visual state with safe fallbacks
                        button.Background = new SolidColorBrush(Colors.Blue);
                        button.Foreground = new SolidColorBrush(Colors.White);
                        System.Diagnostics.Debug.WriteLine($"Button visual state updated to SELECTED");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Button state after: Background={button.Background}, Foreground={button.Foreground}");
                    System.Diagnostics.Debug.WriteLine($"About to call ApplyNewFilteringSystem with {_selectedTags.Count} selected tags");
                    
                    // NOW apply filtering based on the NEW state
                    ApplyNewFilteringSystem();
                    
                    System.Diagnostics.Debug.WriteLine($"=== END NEW FILTERING SYSTEM ===");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in TagFilterButton_Click: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void ApplyNewFilteringSystem()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== APPLYING NEW FILTERING SYSTEM ===");
                System.Diagnostics.Debug.WriteLine($"Current selected tags: [{string.Join(", ", _selectedTags)}]");
                System.Diagnostics.Debug.WriteLine($"Search text: '{SearchBox?.Text ?? ""}'");
                System.Diagnostics.Debug.WriteLine($"Show favorites only: {_showFavoritesOnly}");
                System.Diagnostics.Debug.WriteLine($"Total snippets available: {_allSnippets?.Count ?? 0}");
                
                if (_allSnippets == null || _allSnippets.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No snippets to filter");
                    return;
                }
                
                // Start with all snippets
                var visibleSnippets = _allSnippets.ToList();
                
                // Apply search filter first
                if (!string.IsNullOrWhiteSpace(SearchBox?.Text))
                {
                    var searchTerm = SearchBox.Text.ToLowerInvariant();
                    System.Diagnostics.Debug.WriteLine($"Applying search filter for term: '{searchTerm}'");
                    
                    var beforeSearch = visibleSnippets.Count;
                    visibleSnippets = visibleSnippets.Where(snippet =>
                    {
                        bool matchesTitle = snippet.Title?.ToLowerInvariant().Contains(searchTerm) == true;
                        bool matchesCode = snippet.Code?.ToLowerInvariant().Contains(searchTerm) == true;
                        bool matchesTags = snippet.Tags?.Any(tag => tag?.ToLowerInvariant().Contains(searchTerm) == true) == true;
                        
                        bool matches = matchesTitle || matchesCode || matchesTags;
                        System.Diagnostics.Debug.WriteLine($"  Snippet '{snippet.Title}' - matchesTitle: {matchesTitle}, matchesCode: {matchesCode}, matchesTags: {matchesTags} -> {matches}");
                        
                        return matches;
                    }).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Search filter: {beforeSearch} -> {visibleSnippets.Count} snippets");
                }
                
                // Apply tag filter
                if (_selectedTags != null && _selectedTags.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Applying tag filter with {_selectedTags.Count} selected tags: {string.Join(", ", _selectedTags)}");
                    
                    var beforeTagFilter = visibleSnippets.Count;
                    
                    // Show snippets that have ANY of the selected tags
                    visibleSnippets = visibleSnippets.Where(snippet => 
                    {
                        if (snippet?.Tags == null)
                        {
                            return false;
                        }
                        
                        bool hasMatchingTag = snippet.Tags.Any(tag => _selectedTags.Contains(tag));
                        System.Diagnostics.Debug.WriteLine($"  Snippet '{snippet.Title}' with tags [{string.Join(", ", snippet.Tags)}] - Has matching tag: {hasMatchingTag}");
                        
                        return hasMatchingTag;
                    }).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Tag filter: {beforeTagFilter} -> {visibleSnippets.Count} snippets");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No tags selected - showing all snippets after search filter");
                }

                // Apply favorites filter
                if (_showFavoritesOnly)
                {
                    System.Diagnostics.Debug.WriteLine($"Applying favorites filter");
                    
                    var beforeFavoritesFilter = visibleSnippets.Count;
                    visibleSnippets = visibleSnippets.Where(snippet => snippet.IsFavorite).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Favorites filter: {beforeFavoritesFilter} -> {visibleSnippets.Count} snippets");
                }
                
                // Log which snippets remain after filtering
                if (visibleSnippets.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Remaining snippets after all filters:");
                    foreach (var snippet in visibleSnippets)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✓ {snippet.Title} (Tags: [{string.Join(", ", snippet.Tags)}])");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("NO SNIPPETS REMAIN AFTER FILTERING!");
                }
                
                // Update UI: Direct and immediate
                System.Diagnostics.Debug.WriteLine($"Updating ListView to show {visibleSnippets.Count} snippets");
                System.Diagnostics.Debug.WriteLine($"ListView.Items.Count before update: {SnippetsListView?.Items?.Count ?? 0}");
                
                // Clear and set new source
                SnippetsListView.ItemsSource = null;
                System.Diagnostics.Debug.WriteLine("ListView.ItemsSource set to null");
                SnippetsListView.ItemsSource = visibleSnippets;
                System.Diagnostics.Debug.WriteLine($"ListView.ItemsSource set to {visibleSnippets.Count} snippets");
                
                // Force immediate visual update
                SnippetsListView.Items.Refresh();
                System.Diagnostics.Debug.WriteLine("ListView.Items.Refresh() called");
                
                // CRITICAL: Apply current theme to maintain dark mode consistency
                if (_isDarkTheme)
                {
                    System.Diagnostics.Debug.WriteLine("Applying dark theme to maintain consistency");
                    UpdateSnippetElementsTheme(true);
                }
                
                System.Diagnostics.Debug.WriteLine($"ListView now contains {SnippetsListView.Items.Count} items");
                System.Diagnostics.Debug.WriteLine("=== NEW FILTERING SYSTEM COMPLETED ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyNewFilteringSystem: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchBox != null)
                    SearchBox.Text = string.Empty;
                    
                _selectedTags?.Clear();
                
                // Update tag filter buttons to show unselected state
                UpdateTagsFilter();
                
                // Apply new filtering system to show all snippets
                ApplyNewFilteringSystem();
                
                System.Diagnostics.Debug.WriteLine("Filters cleared successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearFiltersButton_Click: {ex.Message}");
            }
        }

        private void UpdateSearchHistoryUI()
        {
            try
            {
                if (SearchHistoryListBox != null && SearchHistoryPanel != null)
                {
                    // Update listbox items
                    SearchHistoryListBox.ItemsSource = null;
                    SearchHistoryListBox.ItemsSource = _searchHistory;
                    
                    // Show/hide panel based on history count
                    if (_searchHistory.Count > 0)
                    {
                        SearchHistoryPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SearchHistoryPanel.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateSearchHistoryUI: {ex.Message}");
            }
        }

        private void SearchHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SearchHistoryListBox?.SelectedItem is string selectedSearch)
                {
                    // Set the search text
                    if (SearchBox != null)
                    {
                        SearchBox.Text = selectedSearch;
                        SearchBox.Focus();
                        
                        // Trigger search immediately
                        ApplyNewFilteringSystem();
                        
                        System.Diagnostics.Debug.WriteLine($"Selected search from history: '{selectedSearch}'");
                    }
                    
                    // Clear selection
                    SearchHistoryListBox.SelectedItem = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SearchHistoryListBox_SelectionChanged: {ex.Message}");
            }
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _searchHistory.Clear();
                UpdateSearchHistoryUI();
                System.Diagnostics.Debug.WriteLine("Search history cleared");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ClearHistoryButton_Click: {ex.Message}");
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show save file dialog
                var saveDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"CopyPasta_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Get all snippets
                    var allSnippets = await _snippetService.GetAllSnippetsAsync();
                    
                    // Serialize to JSON
                    var json = JsonConvert.SerializeObject(allSnippets, Formatting.Indented);
                    
                    // Save to file
                    await File.WriteAllTextAsync(saveDialog.FileName, json);
                    
                    // Show success message
                    MessageBox.Show(
                        $"Successfully exported {allSnippets.Count} snippet(s) to:\n{saveDialog.FileName}",
                        "Export Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"Exported {allSnippets.Count} snippets to {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting snippets: {ex.Message}");
                MessageBox.Show(
                    $"Failed to export snippets:\n{ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show open file dialog
                var openDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openDialog.ShowDialog() == true)
                {
                    // Read the JSON file
                    var json = await File.ReadAllTextAsync(openDialog.FileName);
                    
                    // Deserialize to list of snippets
                    var importedSnippets = JsonConvert.DeserializeObject<List<Snippet>>(json);
                    
                    if (importedSnippets == null || importedSnippets.Count == 0)
                    {
                        MessageBox.Show(
                            "No snippets found in the selected file.",
                            "Import Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    // Ask user how to import
                    var result = MessageBox.Show(
                        $"Found {importedSnippets.Count} snippet(s) in the file.\n\n" +
                        "How would you like to import them?\n\n" +
                        "Yes - Replace all existing snippets\n" +
                        "No - Add to existing snippets\n" +
                        "Cancel - Don't import",
                        "Import Snippets",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question
                    );

                    if (result == MessageBoxResult.Cancel)
                        return;

                    if (result == MessageBoxResult.Yes)
                    {
                        // Replace mode: Delete all existing, then add imported
                        foreach (var existingSnippet in _allSnippets)
                        {
                            await _snippetService.DeleteSnippetAsync(existingSnippet.Id);
                        }
                        
                        foreach (var snippet in importedSnippets)
                        {
                            snippet.Id = Guid.NewGuid().ToString(); // Generate new ID
                            await _snippetService.AddSnippetAsync(snippet);
                        }
                    }
                    else if (result == MessageBoxResult.No)
                    {
                        // Add mode: Just add imported snippets
                        foreach (var snippet in importedSnippets)
                        {
                            snippet.Id = Guid.NewGuid().ToString(); // Generate new ID
                            await _snippetService.AddSnippetAsync(snippet);
                        }
                    }

                    // Reload snippets in UI
                    await LoadSnippetsAsync();
                    UpdateTagsFilter();
                    ApplyNewFilteringSystem();
                    
                    // Apply theme if in dark mode
                    if (_isDarkTheme)
                    {
                        UpdateSnippetElementsTheme(true);
                    }

                    MessageBox.Show(
                        $"Successfully imported {importedSnippets.Count} snippet(s).",
                        "Import Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    
                    System.Diagnostics.Debug.WriteLine($"Imported {importedSnippets.Count} snippets from {openDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing snippets: {ex.Message}");
                MessageBox.Show(
                    $"Failed to import snippets:\n{ex.Message}",
                    "Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void ReloadSampleDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ReloadSampleDataButton_Click: Starting immediate sample data refresh");
                
                // Force immediate use of fresh sample data directly in UI
                ForceFreshSampleDataInUI();
                
                System.Diagnostics.Debug.WriteLine("ReloadSampleDataButton_Click: Sample data immediately refreshed");
                
                // Show feedback to user
                MessageBox.Show("Sample data has been immediately refreshed with PowerShell and Java snippets for testing.", 
                              "Sample Data Refreshed", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ReloadSampleDataButton_Click: {ex.Message}");
                MessageBox.Show($"Error refreshing sample data: {ex.Message}", 
                              "Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        }

        private void ForceFreshSampleDataInUI()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ForceFreshSampleDataInUI: Creating fresh sample data directly in UI");
                
                // Create fresh sample data directly
                _allSnippets = new List<Snippet>
                {
                    new Snippet(
                        "React useState Hook",
                        "javascript",
                        new List<string> { "react", "hooks", "state" },
                        "import { useState } from 'react';\n\nfunction Example() {\n  const [count, setCount] = useState(0);\n  \n  return (\n    <div>\n      <p>You clicked {count} times</p>\n      <button onClick={() => setCount(count + 1)}>\n        Click me\n      </button>\n    </div>\n  );\n}"
                    ),
                    new Snippet(
                        "Python List Comprehension",
                        "python",
                        new List<string> { "python", "list", "comprehension" },
                        "# Basic list comprehension\nsquares = [x**2 for x in range(10)]\n\n# With condition\neven_squares = [x**2 for x in range(10) if x % 2 == 0]\n\n# Nested comprehension\nmatrix = [[i+j for j in range(3)] for i in range(3)]"
                    ),
                    new Snippet(
                        "CSS Flexbox Center",
                        "css",
                        new List<string> { "css", "flexbox", "layout" },
                        ".container {\n  display: flex;\n  justify-content: center;\n  align-items: center;\n  min-height: 100vh;\n}\n\n.item {\n  /* Your content here */\n}"
                    ),
                    new Snippet(
                        "PowerShell Get-Process",
                        "powershell",
                        new List<string> { "PS", "powershell", "process" },
                        "# Get all running processes\nGet-Process | Where-Object {$_.CPU -gt 10} | Sort-Object CPU -Descending\n\n# Get specific process by name\nGet-Process -Name 'notepad' -ErrorAction SilentlyContinue\n\n# Get process with custom properties\nGet-Process | Select-Object Name, Id, CPU, WorkingSet | Format-Table -AutoSize"
                    ),
                    new Snippet(
                        "Java Stream API Example",
                        "java",
                        new List<string> { "java", "stream", "collections" },
                        "import java.util.List;\nimport java.util.stream.Collectors;\n\n// Filter and map using streams\nList<String> names = List.of(\"Alice\", \"Bob\", \"Charlie\", \"David\");\nList<String> filteredNames = names.stream()\n    .filter(name -> name.length() > 4)\n    .map(String::toUpperCase)\n    .collect(Collectors.toList());\n\nSystem.out.println(filteredNames); // [ALICE, CHARLIE, DAVID]"
                    )
                };
                
                // Update filtered snippets
                _filteredSnippets = _allSnippets.ToList();
                
                System.Diagnostics.Debug.WriteLine($"ForceFreshSampleDataInUI: Created {_allSnippets.Count} fresh sample snippets");
                foreach (var snippet in _allSnippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
                
                // Update UI immediately using direct manipulation
                UpdateSnippetsDisplayDirect();
                
                // CRITICAL: Apply current theme to maintain dark mode consistency
                if (_isDarkTheme)
                {
                    System.Diagnostics.Debug.WriteLine("Applying dark theme to maintain consistency after sample data refresh");
                    UpdateSnippetElementsTheme(true);
                }
                
                System.Diagnostics.Debug.WriteLine("ForceFreshSampleDataInUI: UI updated with fresh sample data");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ForceFreshSampleDataInUI: {ex.Message}");
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            
            if (_isDarkTheme)
            {
                // Dark theme
                ThemeToggleButton.Content = "☀️";
                this.Background = new SolidColorBrush(Color.FromRgb(33, 33, 33)); // Dark gray
                this.Foreground = new SolidColorBrush(Colors.White);
                
                // Update main content area
                if (MainContentGrid != null)
                {
                    MainContentGrid.Background = new SolidColorBrush(Color.FromRgb(48, 48, 48)); // Darker gray
                }
                
                // Update all snippet elements for dark theme
                UpdateSnippetElementsTheme(true);
            }
            else
            {
                // Light theme
                ThemeToggleButton.Content = "🌙";
                this.Background = new SolidColorBrush(Colors.White);
                this.Foreground = new SolidColorBrush(Colors.Black);
                
                // Update main content area
                if (MainContentGrid != null)
                {
                    MainContentGrid.Background = new SolidColorBrush(Colors.White);
                }
                
                // Update all snippet elements for light theme
                UpdateSnippetElementsTheme(false);
            }
        }
        
        private void UpdateSnippetElementsTheme(bool isDark)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== UPDATING THEME TO {(isDark ? "DARK" : "LIGHT")} ===");
                
                // Update tag filter buttons
                foreach (var child in TagsFilterPanel.Children)
                {
                    if (child is Button button)
                    {
                        if (isDark)
                        {
                            button.Foreground = new SolidColorBrush(Colors.White);
                            button.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)); // Dark gray
                        }
                        else
                        {
                            button.Foreground = new SolidColorBrush(Colors.Black);
                            button.Background = new SolidColorBrush(Colors.White);
                        }
                    }
                }
                
                // Update search box and clear filters button
                if (SearchBox != null)
                {
                    SearchBox.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    SearchBox.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                }
                
                if (ClearFiltersButton != null)
                {
                    ClearFiltersButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    ClearFiltersButton.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                }
                
                if (ReloadSampleDataButton != null)
                {
                    ReloadSampleDataButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    ReloadSampleDataButton.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                }
                
                if (ExportButton != null)
                {
                    ExportButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    ExportButton.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                }
                
                if (ImportButton != null)
                {
                    ImportButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    ImportButton.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                }
                
                if (ShowFavoritesOnlyCheckBox != null)
                {
                    ShowFavoritesOnlyCheckBox.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                }
                
                if (MultiSelectModeCheckBox != null)
                {
                    MultiSelectModeCheckBox.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                }
                
                if (SelectAllButton != null)
                {
                    SelectAllButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    SelectAllButton.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                }
                
                if (DeselectAllButton != null)
                {
                    DeselectAllButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    DeselectAllButton.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                }
                
                if (SelectedCountText != null)
                {
                    SelectedCountText.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                }
                
                // Update Search History panel
                if (SearchHistoryPanel != null)
                {
                    SearchHistoryPanel.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                    SearchHistoryPanel.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                }
                
                if (SearchHistoryListBox != null)
                {
                    SearchHistoryListBox.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                    SearchHistoryListBox.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                }
                
                if (ClearHistoryButton != null)
                {
                    ClearHistoryButton.Foreground = isDark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
                    ClearHistoryButton.Background = isDark ? new SolidColorBrush(Color.FromRgb(64, 64, 64)) : new SolidColorBrush(Colors.White);
                }
                
                // Update Statistics panel
                UpdateStatisticsPanelTheme(isDark);
                
                // Update left panel background
                if (LeftPanel != null)
                {
                    LeftPanel.Background = isDark ? new SolidColorBrush(Colors.Transparent) : new SolidColorBrush(Colors.White);
                }
                
                // Update main content grid background
                if (MainContentGrid != null)
                {
                    MainContentGrid.Background = isDark ? new SolidColorBrush(Color.FromRgb(48, 48, 48)) : new SolidColorBrush(Colors.White);
                }
                
                // Update snippet cards and their content
                UpdateSnippetCardsTheme(isDark);
                
                // Update scrollbar colors for visibility
                UpdateScrollBarTheme(isDark);
                
                System.Diagnostics.Debug.WriteLine($"Updated theme for all snippet elements: {(isDark ? "Dark" : "Light")}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating snippet elements theme: {ex.Message}");
            }
        }
        
        private void UpdateSnippetCardsTheme(bool isDark)
        {
            try
            {
                if (SnippetsListView != null && SnippetsListView.ItemsSource != null)
                {
                    // Force refresh of the ItemsControl to trigger theme updates
                    var currentSource = SnippetsListView.ItemsSource;
                    SnippetsListView.ItemsSource = null;
                    SnippetsListView.ItemsSource = currentSource;
                    
                    // Update the visual style of snippet cards
                    if (isDark)
                    {
                        // Dark theme colors for snippet cards
                        SnippetsListView.Background = new SolidColorBrush(Color.FromRgb(48, 48, 48));
                        
                        // Update selection highlight colors for dark theme
                        Application.Current.Resources["SelectionHighlight"] = new SolidColorBrush(Color.FromRgb(80, 80, 140)); // Dark blue
                        Application.Current.Resources["SelectionBorder"] = new SolidColorBrush(Color.FromRgb(120, 120, 200)); // Lighter blue
                        
                        // Force update of Material Design resources for dark theme
                        Application.Current.Resources["MaterialDesignPaper"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                        Application.Current.Resources["MaterialDesignBody"] = new SolidColorBrush(Colors.White);
                        Application.Current.Resources["MaterialDesignDivider"] = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                    }
                    else
                    {
                        // Light theme colors for snippet cards
                        SnippetsListView.Background = new SolidColorBrush(Colors.White);
                        
                        // Update selection highlight colors for light theme
                        Application.Current.Resources["SelectionHighlight"] = new SolidColorBrush(Color.FromArgb(100, 255, 215, 0)); // Light gold
                        Application.Current.Resources["SelectionBorder"] = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Gold border
                        
                        // Force update of Material Design resources for light theme
                        Application.Current.Resources["MaterialDesignPaper"] = new SolidColorBrush(Colors.White);
                        Application.Current.Resources["MaterialDesignBody"] = new SolidColorBrush(Colors.Black);
                        Application.Current.Resources["MaterialDesignDivider"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    }
                    
                    // Update ListViewItem selection styling
                    UpdateListViewItemSelectionStyle(isDark);
                    
                    // Force a complete visual refresh
                    SnippetsListView.InvalidateVisual();
                    SnippetsListView.UpdateLayout();
                    
                    // Schedule a delayed update to ensure all elements are rendered
                    Dispatcher.BeginInvoke(new Action(() => ForceUpdateSnippetCards(isDark)), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating snippet cards theme: {ex.Message}");
            }
        }
        
        private void UpdateListViewItemSelectionStyle(bool isDark)
        {
            try
            {
                // Update the selection highlight color for ListViewItems
                var highlightColor = isDark ? Color.FromArgb(80, 70, 130, 180) : Color.FromArgb(150, 255, 215, 0);
                var borderColor = isDark ? Color.FromRgb(100, 150, 200) : Color.FromRgb(255, 215, 0);
                
                // Find all ListViewItems and update their selection styling
                if (SnippetsListView != null)
                {
                    foreach (ListViewItem item in SnippetsListView.Items)
                    {
                        if (item.IsSelected)
                        {
                            item.Background = new SolidColorBrush(highlightColor);
                            item.BorderBrush = new SolidColorBrush(borderColor);
                            item.BorderThickness = new Thickness(3);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ListViewItem selection style: {ex.Message}");
            }
        }

        private void UpdateScrollBarTheme(bool isDark)
        {
            try
            {
                if (isDark)
                {
                    // Light gray scrollbar for dark theme
                    Application.Current.Resources["ScrollBar.StaticThumb"] = new SolidColorBrush(Colors.LightGray);
                    Application.Current.Resources["ScrollBar.MouseOverThumb"] = new SolidColorBrush(Colors.White);
                    Application.Current.Resources["ScrollBar.PressedThumb"] = new SolidColorBrush(Colors.White);
                    Application.Current.Resources["ScrollBar.StaticTrackBackground"] = new SolidColorBrush(Color.FromRgb(40, 40, 40));
                }
                else
                {
                    // Darker scrollbar for light theme
                    Application.Current.Resources["ScrollBar.StaticThumb"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                    Application.Current.Resources["ScrollBar.MouseOverThumb"] = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                    Application.Current.Resources["ScrollBar.PressedThumb"] = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                    Application.Current.Resources["ScrollBar.StaticTrackBackground"] = new SolidColorBrush(Colors.LightGray);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating scrollbar theme: {ex.Message}");
            }
        }

        private void ForceUpdateSnippetCards(bool isDark)
        {
            try
            {
                // Find all snippet cards and force update their colors
                var snippetCards = FindVisualChildren<MaterialDesignThemes.Wpf.Card>(SnippetsListView);
                foreach (var card in snippetCards)
                {
                    if (isDark)
                    {
                        // Use darker background that matches the rest of the app
                        card.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                        card.Foreground = new SolidColorBrush(Colors.White);
                        card.BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    }
                    else
                    {
                        card.Background = new SolidColorBrush(Colors.White);
                        card.Foreground = new SolidColorBrush(Colors.Black);
                        card.BorderBrush = new SolidColorBrush(Colors.LightGray);
                    }
                }
                
                // Find all text blocks and update their colors
                var textBlocks = FindVisualChildren<TextBlock>(SnippetsListView);
                foreach (var textBlock in textBlocks)
                {
                    if (isDark)
                    {
                        textBlock.Foreground = new SolidColorBrush(Colors.White);
                    }
                    else
                    {
                        textBlock.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }
                
                // Find all borders (tags) and update their colors
                var borders = FindVisualChildren<Border>(SnippetsListView);
                foreach (var border in borders)
                {
                    if (isDark)
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    }
                    else
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                    }
                }
                
                // Find all TextBoxes (if any remain) and update their colors
                var textBoxes = FindVisualChildren<TextBox>(SnippetsListView);
                foreach (var textBox in textBoxes)
                {
                    if (isDark)
                    {
                        textBox.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)); // Dark gray background
                        textBox.Foreground = new SolidColorBrush(Colors.White); // WHITE TEXT for dark mode
                        textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Light gray border
                    }
                    else
                    {
                        textBox.Background = new SolidColorBrush(Colors.White); // White background
                        textBox.Foreground = new SolidColorBrush(Colors.Black); // BLACK TEXT for light mode
                        textBox.BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)); // Light gray border
                    }
                }
                
                // Find all TextEditors (AvalonEdit) and update their colors
                var textEditors = FindVisualChildren<ICSharpCode.AvalonEdit.TextEditor>(SnippetsListView);
                foreach (var textEditor in textEditors)
                {
                    if (isDark)
                    {
                        textEditor.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)); // Dark gray background
                        textEditor.Foreground = new SolidColorBrush(Colors.White); // WHITE TEXT for dark mode
                    }
                    else
                    {
                        textEditor.Background = new SolidColorBrush(Colors.White); // White background
                        textEditor.Foreground = new SolidColorBrush(Colors.Black); // BLACK TEXT for light mode
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Force updated {snippetCards.Count()} snippet cards, {textBoxes.Count()} text boxes, {textEditors.Count()} text editors for {(isDark ? "dark" : "light")} theme");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error force updating snippet cards: {ex.Message}");
            }
        }
        
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var children = new List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    children.Add(result);
                
                children.AddRange(FindVisualChildren<T>(child));
            }
            return children;
        }

        private async void NewSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            var snippetDialog = new SnippetDialog();
            if (snippetDialog.ShowDialog() == true)
            {
                var newSnippet = snippetDialog.Snippet;
                
                // Check for potential duplicates
                var potentialDuplicates = await _snippetService.FindPotentialDuplicatesAsync(newSnippet);
                
                if (potentialDuplicates.Count > 0)
                {
                    // Build message showing duplicate details
                    var message = $"Found {potentialDuplicates.Count} potential duplicate snippet(s):\n\n";
                    foreach (var dup in potentialDuplicates.Take(5)) // Show max 5 duplicates
                    {
                        message += $"• {dup.Title} ({dup.Language})\n";
                    }
                    if (potentialDuplicates.Count > 5)
                    {
                        message += $"... and {potentialDuplicates.Count - 5} more\n";
                    }
                    message += "\nDo you still want to create this snippet?";
                    
                    var result = MessageBox.Show(
                        message,
                        "Potential Duplicate Detected",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );
                    
                    if (result == MessageBoxResult.No)
                    {
                        System.Diagnostics.Debug.WriteLine("User cancelled creating duplicate snippet");
                        return; // User cancelled
                    }
                }
                
                // Proceed with creating the snippet
                await _snippetService.AddSnippetAsync(newSnippet);
                await LoadSnippetsAsync();
                UpdateTagsFilter();
                ApplyNewFilteringSystem();
                UpdateStatistics(); // Update statistics after adding
                
                // CRITICAL: Apply current theme to maintain consistency after adding new snippet
                if (_isDarkTheme)
                {
                    System.Diagnostics.Debug.WriteLine("Applying dark theme after creating new snippet");
                    UpdateSnippetElementsTheme(true);
                }
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Snippet snippet)
            {
                var snippetDialog = new SnippetDialog(snippet);
                if (snippetDialog.ShowDialog() == true)
                {
                    var updatedSnippet = snippetDialog.Snippet;
                    
                    // Check for potential duplicates (excluding the current snippet being edited)
                    var potentialDuplicates = await _snippetService.FindPotentialDuplicatesAsync(updatedSnippet);
                    
                    if (potentialDuplicates.Count > 0)
                    {
                        // Build message showing duplicate details
                        var message = $"Found {potentialDuplicates.Count} potential duplicate snippet(s):\n\n";
                        foreach (var dup in potentialDuplicates.Take(5)) // Show max 5 duplicates
                        {
                            message += $"• {dup.Title} ({dup.Language})\n";
                        }
                        if (potentialDuplicates.Count > 5)
                        {
                            message += $"... and {potentialDuplicates.Count - 5} more\n";
                        }
                        message += "\nDo you still want to save these changes?";
                        
                        var result = MessageBox.Show(
                            message,
                            "Potential Duplicate Detected",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning
                        );
                        
                        if (result == MessageBoxResult.No)
                        {
                            System.Diagnostics.Debug.WriteLine("User cancelled saving duplicate snippet");
                            return; // User cancelled
                        }
                    }
                    
                    // Proceed with updating the snippet
                    await _snippetService.UpdateSnippetAsync(updatedSnippet);
                    await LoadSnippetsAsync();
                    UpdateTagsFilter();
                    ApplyNewFilteringSystem();
                    UpdateStatistics(); // Update statistics after editing
                    
                    // CRITICAL: Apply current theme to maintain consistency after editing snippet
                    if (_isDarkTheme)
                    {
                        System.Diagnostics.Debug.WriteLine("Applying dark theme after editing snippet");
                        UpdateSnippetElementsTheme(true);
                    }
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
                            ApplyNewFilteringSystem(); // Use new filtering system
                            UpdateStatistics(); // Update statistics after deleting
                            
                            // CRITICAL: Apply current theme to maintain consistency after deleting snippet
                            if (_isDarkTheme)
                            {
                                System.Diagnostics.Debug.WriteLine("Applying dark theme after deleting snippet");
                                UpdateSnippetElementsTheme(true);
                            }
                        });
                    });
                }
            }
        }

        private async void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Snippet snippet)
                {
                    // Toggle favorite status
                    snippet.IsFavorite = !snippet.IsFavorite;
                    
                    // Save the change
                    await _snippetService.UpdateSnippetAsync(snippet);
                    
                    // Reload snippets to reflect changes
                    await LoadSnippetsAsync();
                    ApplyNewFilteringSystem();
                    
                    System.Diagnostics.Debug.WriteLine($"Toggled favorite for snippet: {snippet.Title} (IsFavorite: {snippet.IsFavorite})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling favorite: {ex.Message}");
                MessageBox.Show($"Failed to toggle favorite: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowFavoritesOnly_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is CheckBox checkBox)
                {
                    _showFavoritesOnly = checkBox.IsChecked == true;
                    ApplyNewFilteringSystem();
                    System.Diagnostics.Debug.WriteLine($"Show favorites only: {_showFavoritesOnly}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing favorites filter: {ex.Message}");
            }
        }

        private void MultiSelectMode_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is CheckBox checkBox)
                {
                    _isMultiSelectMode = checkBox.IsChecked == true;
                    
                    // Show/hide bulk actions panel
                    if (BulkActionsPanel != null)
                    {
                        BulkActionsPanel.Visibility = _isMultiSelectMode ? Visibility.Visible : Visibility.Collapsed;
                    }
                    
                    // Update ListView selection mode
                    if (SnippetsListView != null)
                    {
                        SnippetsListView.SelectionMode = _isMultiSelectMode ? SelectionMode.Extended : SelectionMode.Single;
                        
                        // Clear selections when exiting multi-select mode
                        if (!_isMultiSelectMode)
                        {
                            SnippetsListView.SelectedItems.Clear();
                        }
                    }
                    
                    UpdateSelectedCount();
                    System.Diagnostics.Debug.WriteLine($"Multi-select mode: {_isMultiSelectMode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error changing multi-select mode: {ex.Message}");
            }
        }

        private void SnippetsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                UpdateSelectedCount();
                
                // Update visual highlighting for all items in the ListView
                if (SnippetsListView != null && _isMultiSelectMode)
                {
                    var highlightColor = _isDarkTheme ? Color.FromArgb(80, 70, 130, 180) : Color.FromArgb(150, 255, 215, 0);
                    var borderColor = _isDarkTheme ? Color.FromRgb(100, 150, 200) : Color.FromRgb(255, 215, 0);
                    
                    // Update all ListViewItems
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var items = FindVisualChildren<ListViewItem>(SnippetsListView);
                        foreach (var item in items)
                        {
                            if (item.IsSelected)
                            {
                                item.Background = new SolidColorBrush(highlightColor);
                                item.BorderBrush = new SolidColorBrush(borderColor);
                                item.BorderThickness = new Thickness(3);
                            }
                            else
                            {
                                item.Background = new SolidColorBrush(Colors.Transparent);
                                item.BorderBrush = null;
                                item.BorderThickness = new Thickness(0);
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in selection changed: {ex.Message}");
            }
        }

        private void SnippetCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // Only handle when in multi-select mode
                if (!_isMultiSelectMode)
                    return;

                // Find the snippet from the DataContext
                if (sender is FrameworkElement element && element.DataContext is Snippet snippet)
                {
                    // Toggle selection
                    if (SnippetsListView != null)
                    {
                        if (SnippetsListView.SelectedItems.Contains(snippet))
                        {
                            // Deselect
                            SnippetsListView.SelectedItems.Remove(snippet);
                        }
                        else
                        {
                            // Select
                            SnippetsListView.SelectedItems.Add(snippet);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Toggled selection for snippet: {snippet.Title}");
                    }
                }
                
                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in snippet card mouse down: {ex.Message}");
            }
        }

        private void SnippetsListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (SnippetsListView == null) return;

                var scrollViewer = GetChildOfType<ScrollViewer>(SnippetsListView);
                if (scrollViewer != null)
                {
                    // Calculate smoother scroll amount (smaller increment)
                    var offset = scrollViewer.VerticalOffset;
                    var delta = e.Delta > 0 ? -15.0 : 15.0; // Small, consistent increment
                    
                    // Apply smooth scrolling
                    scrollViewer.ScrollToVerticalOffset(offset + delta);
                    
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PreviewMouseWheel: {ex.Message}");
            }
        }

        private static T? GetChildOfType<T>(DependencyObject? depObj) where T : class
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                if (child is T result)
                {
                    return result;
                }

                var childOfType = GetChildOfType<T>(child);
                if (childOfType != null)
                {
                    return childOfType;
                }
            }

            return null;
        }

        private void UpdateSelectedCount()
        {
            try
            {
                if (SelectedCountText != null && SnippetsListView != null)
                {
                    var count = SnippetsListView.SelectedItems.Count;
                    if (count > 0)
                    {
                        SelectedCountText.Text = $"{count} item(s) selected";
                        SelectedCountText.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        SelectedCountText.Text = "";
                        SelectedCountText.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating selected count: {ex.Message}");
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SnippetsListView != null)
                {
                    SnippetsListView.SelectAll();
                    UpdateSelectedCount();
                    System.Diagnostics.Debug.WriteLine("Selected all items");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error selecting all: {ex.Message}");
            }
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SnippetsListView != null)
                {
                    SnippetsListView.SelectedItems.Clear();
                    UpdateSelectedCount();
                    System.Diagnostics.Debug.WriteLine("Deselected all items");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deselecting all: {ex.Message}");
            }
        }

        private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SnippetsListView?.SelectedItems == null || SnippetsListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("No items selected.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var selectedCount = SnippetsListView.SelectedItems.Count;
                var result = MessageBox.Show(
                    $"Are you sure you want to delete {selectedCount} selected snippet(s)?",
                    "Confirm Bulk Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Collect selected snippets
                    var selectedSnippets = SnippetsListView.SelectedItems.Cast<Snippet>().ToList();
                    
                    // Delete each snippet
                    foreach (var snippet in selectedSnippets)
                    {
                        await _snippetService.DeleteSnippetAsync(snippet.Id);
                    }
                    
                    // Reload and refresh
                    await LoadSnippetsAsync();
                    UpdateTagsFilter();
                    ApplyNewFilteringSystem();
                    UpdateStatistics();
                    
                    if (_isDarkTheme)
                    {
                        UpdateSnippetElementsTheme(true);
                    }
                    
                    MessageBox.Show(
                        $"Successfully deleted {selectedCount} snippet(s).",
                        "Bulk Delete Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    System.Diagnostics.Debug.WriteLine($"Bulk deleted {selectedCount} snippets");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in bulk delete: {ex.Message}");
                MessageBox.Show($"Failed to delete selected snippets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Snippet snippet)
            {
                try
                {
                    Clipboard.SetText(snippet.Code);
                    // Copy operation successful - no need for error handling or snackbar
                }
                catch (Exception ex)
                {
                    // Only show error if clipboard operation actually fails
                    MessageBox.Show($"Failed to copy code: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CodeEditor_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is TextEditor editor && editor.DataContext is Snippet snippet)
                {
                    // Set the code text
                    editor.Text = snippet.Code;
                    
                    // Set syntax highlighting based on language
                    SetSyntaxHighlighting(editor, snippet.Language);
                    
                    // Apply theme colors
                    if (_isDarkTheme)
                    {
                        editor.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                        editor.Foreground = new SolidColorBrush(Colors.White);
                    }
                    else
                    {
                        editor.Background = new SolidColorBrush(Colors.White);
                        editor.Foreground = new SolidColorBrush(Colors.Black);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Code editor loaded for snippet: {snippet.Title} (Language: {snippet.Language})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading code editor: {ex.Message}");
            }
        }

        private void SetSyntaxHighlighting(TextEditor editor, string language)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(language))
                {
                    editor.SyntaxHighlighting = null;
                    return;
                }

                var languageLower = language.ToLowerInvariant();
                
                // Map language names to highlighting definitions
                IHighlightingDefinition? highlighting = languageLower switch
                {
                    "javascript" or "js" => HighlightingManager.Instance.GetDefinition("JavaScript"),
                    "typescript" or "ts" => HighlightingManager.Instance.GetDefinition("TypeScript"),
                    "python" => HighlightingManager.Instance.GetDefinition("Python"),
                    "csharp" or "cs" or "c#" => HighlightingManager.Instance.GetDefinition("C#"),
                    "java" => HighlightingManager.Instance.GetDefinition("Java"),
                    "cpp" or "c++" or "cplusplus" => HighlightingManager.Instance.GetDefinition("C++"),
                    "c" => HighlightingManager.Instance.GetDefinition("C"),
                    "xml" => HighlightingManager.Instance.GetDefinition("XML"),
                    "html" => HighlightingManager.Instance.GetDefinition("HTML"),
                    "css" => HighlightingManager.Instance.GetDefinition("CSS"),
                    "json" => HighlightingManager.Instance.GetDefinition("JSON"),
                    "sql" => HighlightingManager.Instance.GetDefinition("SQL"),
                    "powershell" or "ps1" => HighlightingManager.Instance.GetDefinition("PowerShell"),
                    "bash" or "sh" => HighlightingManager.Instance.GetDefinition("Bash"),
                    "php" => HighlightingManager.Instance.GetDefinition("PHP"),
                    "ruby" => HighlightingManager.Instance.GetDefinition("Ruby"),
                    "go" or "golang" => HighlightingManager.Instance.GetDefinition("Go"),
                    "rust" => HighlightingManager.Instance.GetDefinition("Rust"),
                    "swift" => HighlightingManager.Instance.GetDefinition("Swift"),
                    "kotlin" => HighlightingManager.Instance.GetDefinition("Kotlin"),
                    _ => null
                };

                editor.SyntaxHighlighting = highlighting;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting syntax highlighting for language '{language}': {ex.Message}");
                editor.SyntaxHighlighting = null;
            }
        }

        private void ForceListViewRefresh()
        {
            try
            {
                // Force immediate refresh of the ListView
                SnippetsListView.Items.Refresh();
                SnippetsListView.InvalidateVisual();
                SnippetsListView.UpdateLayout();
                
                // Force parent container refresh
                if (SnippetsListView.Parent is FrameworkElement parent)
                {
                    parent.InvalidateVisual();
                    parent.UpdateLayout();
                }
                
                System.Diagnostics.Debug.WriteLine($"FORCE REFRESH: ListView refreshed, Items.Count: {SnippetsListView.Items.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ForceListViewRefresh: {ex.Message}");
            }
        }

        private void LogCurrentState()
        {
            System.Diagnostics.Debug.WriteLine("=== CURRENT STATE LOG ===");
            System.Diagnostics.Debug.WriteLine($"Total snippets: {_allSnippets?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Filtered snippets: {_filteredSnippets?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"Selected tags: [{string.Join("][", _selectedTags ?? new List<string>())}]");
            System.Diagnostics.Debug.WriteLine($"Search text: '{SearchBox?.Text ?? "null"}'");
            
            if (_allSnippets != null)
            {
                System.Diagnostics.Debug.WriteLine("All snippets:");
                foreach (var snippet in _allSnippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
            }
            
            if (_filteredSnippets != null)
            {
                System.Diagnostics.Debug.WriteLine("Filtered snippets:");
                foreach (var snippet in _filteredSnippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
            }
            System.Diagnostics.Debug.WriteLine("=== END STATE LOG ===");
        }
    }
}
