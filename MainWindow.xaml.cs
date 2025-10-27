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
using System.ComponentModel;

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading snippets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                System.Diagnostics.Debug.WriteLine($"Total snippets available: {_allSnippets?.Count ?? 0}");
                
                if (_allSnippets == null || _allSnippets.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No snippets to filter");
                    return;
                }
                
                // Log all available snippets and their tags
                System.Diagnostics.Debug.WriteLine("All available snippets:");
                foreach (var snippet in _allSnippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: Tags=[{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
                
                // BRAND NEW FILTERING LOGIC: Show snippets that have ANY of the selected tags
                List<Snippet> visibleSnippets;
                
                if (_selectedTags.Count == 0)
                {
                    // No tags selected = show all snippets
                    visibleSnippets = _allSnippets.ToList();
                    System.Diagnostics.Debug.WriteLine("No tags selected - showing ALL snippets");
                }
                else
                {
                    // Tags selected = show only snippets with matching tags
                    System.Diagnostics.Debug.WriteLine($"Filtering snippets for tags: [{string.Join(", ", _selectedTags)}]");
                    
                    visibleSnippets = _allSnippets.Where(snippet => 
                    {
                        if (snippet?.Tags == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"  Skipping snippet '{snippet?.Title}' - no tags");
                            return false;
                        }
                        
                        bool hasMatchingTag = snippet.Tags.Any(tag => _selectedTags.Contains(tag));
                        System.Diagnostics.Debug.WriteLine($"  Snippet '{snippet.Title}' with tags [{string.Join(", ", snippet.Tags)}] - Has matching tag: {hasMatchingTag}");
                        
                        return hasMatchingTag;
                    }).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"Tags selected - showing {visibleSnippets.Count} matching snippets:");
                    foreach (var snippet in visibleSnippets)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✓ {snippet.Title} (Tags: [{string.Join(", ", snippet.Tags)}])");
                    }
                }
                
                // BRAND NEW UI UPDATE: Direct and immediate
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
                
                // Update main content grid background
                if (MainContentGrid != null)
                {
                    MainContentGrid.Background = isDark ? new SolidColorBrush(Color.FromRgb(48, 48, 48)) : new SolidColorBrush(Colors.White);
                }
                
                // Update snippet cards and their content
                UpdateSnippetCardsTheme(isDark);
                
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
                        
                        // Force update of Material Design resources for dark theme
                        Application.Current.Resources["MaterialDesignPaper"] = new SolidColorBrush(Color.FromRgb(48, 48, 48));
                        Application.Current.Resources["MaterialDesignBody"] = new SolidColorBrush(Colors.White);
                        Application.Current.Resources["MaterialDesignDivider"] = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    }
                    else
                    {
                        // Light theme colors for snippet cards
                        SnippetsListView.Background = new SolidColorBrush(Colors.White);
                        
                        // Force update of Material Design resources for light theme
                        Application.Current.Resources["MaterialDesignPaper"] = new SolidColorBrush(Colors.White);
                        Application.Current.Resources["MaterialDesignBody"] = new SolidColorBrush(Colors.Black);
                        Application.Current.Resources["MaterialDesignDivider"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    }
                    
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
                        card.Background = new SolidColorBrush(Color.FromRgb(48, 48, 48));
                        card.Foreground = new SolidColorBrush(Colors.White);
                    }
                    else
                    {
                        card.Background = new SolidColorBrush(Colors.White);
                        card.Foreground = new SolidColorBrush(Colors.Black);
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
                
                // Find all TextBoxes (code areas) and update their colors - THIS IS THE KEY FIX!
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
                
                System.Diagnostics.Debug.WriteLine($"Force updated {snippetCards.Count()} snippet cards, {textBoxes.Count()} text boxes for {(isDark ? "dark" : "light")} theme");
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
                        ApplyNewFilteringSystem(); // Use new filtering system
                        
                        // CRITICAL: Apply current theme to maintain consistency after adding new snippet
                        if (_isDarkTheme)
                        {
                            System.Diagnostics.Debug.WriteLine("Applying dark theme after creating new snippet");
                            UpdateSnippetElementsTheme(true);
                        }
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
                            ApplyNewFilteringSystem(); // Use new filtering system
                            
                            // CRITICAL: Apply current theme to maintain consistency after editing snippet
                            if (_isDarkTheme)
                            {
                                System.Diagnostics.Debug.WriteLine("Applying dark theme after editing snippet");
                                UpdateSnippetElementsTheme(true);
                            }
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
                            ApplyNewFilteringSystem(); // Use new filtering system
                            
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
