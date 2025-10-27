using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CopyPastaNative.Models;
using Newtonsoft.Json;

namespace CopyPastaNative.Services
{
    public class SnippetService
    {
        private readonly string _filePath;
        private List<Snippet> _snippets = new();

        public SnippetService()
        {
            _filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CopyPasta",
                "snippets.json"
            );
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public async Task<List<Snippet>> GetAllSnippetsAsync()
        {
            if (_snippets.Count == 0)
            {
                await LoadSnippetsAsync();
            }
            
            // Debug: Log what snippets we have
            System.Diagnostics.Debug.WriteLine($"GetAllSnippetsAsync: Returning {_snippets.Count} snippets");
            foreach (var snippet in _snippets)
            {
                System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
            }
            
            return _snippets.ToList();
        }

        public async Task<Snippet?> GetSnippetByIdAsync(string id)
        {
            if (_snippets.Count == 0)
            {
                await LoadSnippetsAsync();
            }
            return _snippets.FirstOrDefault(s => s.Id == id);
        }

        public async Task AddSnippetAsync(Snippet snippet)
        {
            snippet.UpdatedAt = DateTime.Now;
            _snippets.Add(snippet);
            await SaveSnippetsAsync();
        }

        public async Task UpdateSnippetAsync(Snippet snippet)
        {
            var existing = _snippets.FirstOrDefault(s => s.Id == snippet.Id);
            if (existing != null)
            {
                existing.Title = snippet.Title;
                existing.Language = snippet.Language;
                existing.Tags = snippet.Tags;
                existing.Code = snippet.Code;
                existing.IsFavorite = snippet.IsFavorite;
                existing.UpdatedAt = DateTime.Now;
                await SaveSnippetsAsync();
            }
        }

        public async Task DeleteSnippetAsync(string id)
        {
            var snippet = _snippets.FirstOrDefault(s => s.Id == id);
            if (snippet != null)
            {
                _snippets.Remove(snippet);
                await SaveSnippetsAsync();
            }
        }

        public async Task<List<Snippet>> SearchSnippetsAsync(string searchTerm)
        {
            if (_snippets.Count == 0)
            {
                await LoadSnippetsAsync();
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
                return _snippets.ToList();

            searchTerm = searchTerm.ToLowerInvariant();
            return _snippets.Where(s =>
                s.Title?.ToLowerInvariant().Contains(searchTerm) == true ||
                s.Code?.ToLowerInvariant().Contains(searchTerm) == true ||
                s.Tags?.Any(tag => tag?.ToLowerInvariant().Contains(searchTerm) == true) == true
            ).ToList();
        }

        public async Task<List<Snippet>> GetSnippetsByTagsAsync(List<string> tags)
        {
            if (_snippets.Count == 0)
            {
                await LoadSnippetsAsync();
            }

            if (tags == null || tags.Count == 0)
                return _snippets.ToList();

            return _snippets.Where(s => 
                s.Tags != null && tags.All(tag => s.Tags.Contains(tag))
            ).ToList();
        }

        public List<string> GetAllTags()
        {
            try
            {
                // Safety check - return empty list if no snippets
                if (_snippets == null || _snippets.Count == 0)
                    return new List<string>();
                    
                return _snippets
                    .Where(s => s.Tags != null) // Filter out snippets with null tags
                    .SelectMany(s => s.Tags)
                    .Where(tag => !string.IsNullOrEmpty(tag)) // Filter out null/empty tags
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Log error and return empty list instead of crashing
                System.Diagnostics.Debug.WriteLine($"Error getting all tags: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<Snippet>> FindPotentialDuplicatesAsync(Snippet newSnippet)
        {
            if (_snippets.Count == 0)
            {
                await LoadSnippetsAsync();
            }

            if (newSnippet == null)
                return new List<Snippet>();

            var potentialDuplicates = new List<Snippet>();
            
            foreach (var existingSnippet in _snippets)
            {
                // Skip if it's the same snippet (when editing)
                if (existingSnippet.Id == newSnippet.Id)
                    continue;

                double similarity = CalculateSimilarity(existingSnippet, newSnippet);
                
                // Consider it a potential duplicate if similarity is >= 70%
                if (similarity >= 0.70)
                {
                    potentialDuplicates.Add(existingSnippet);
                }
            }

            return potentialDuplicates.OrderByDescending(s => 
                CalculateSimilarity(s, newSnippet)
            ).ToList();
        }

        private double CalculateSimilarity(Snippet snippet1, Snippet snippet2)
        {
            double totalSimilarity = 0.0;
            int checks = 0;

            // Compare titles (40% weight)
            if (!string.IsNullOrWhiteSpace(snippet1.Title) && !string.IsNullOrWhiteSpace(snippet2.Title))
            {
                double titleSimilarity = CalculateStringSimilarity(
                    snippet1.Title.ToLowerInvariant(), 
                    snippet2.Title.ToLowerInvariant()
                );
                totalSimilarity += titleSimilarity * 0.4;
                checks++;
            }

            // Compare code content (60% weight)
            if (!string.IsNullOrWhiteSpace(snippet1.Code) && !string.IsNullOrWhiteSpace(snippet2.Code))
            {
                double codeSimilarity = CalculateStringSimilarity(
                    snippet1.Code.ToLowerInvariant(), 
                    snippet2.Code.ToLowerInvariant()
                );
                totalSimilarity += codeSimilarity * 0.6;
                checks++;
            }

            return checks > 0 ? totalSimilarity : 0.0;
        }

        private double CalculateStringSimilarity(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0.0;

            if (str1.Equals(str2, StringComparison.OrdinalIgnoreCase))
                return 1.0;

            // Use Levenshtein distance for similarity calculation
            int maxLength = Math.Max(str1.Length, str2.Length);
            if (maxLength == 0)
                return 1.0;

            int distance = LevenshteinDistance(str1, str2);
            double similarity = 1.0 - ((double)distance / maxLength);
            
            return Math.Max(0.0, similarity);
        }

        private int LevenshteinDistance(string str1, string str2)
        {
            int n = str1.Length;
            int m = str2.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0)
                return m;
            if (m == 0)
                return n;

            for (int i = 0; i <= n; i++)
                d[i, 0] = i;

            for (int j = 0; j <= m; j++)
                d[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (str2[j - 1] == str1[i - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[n, m];
        }

        public async Task ForceReloadSampleData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ForceReloadSampleData: Deleting existing snippets.json");
                
                // Delete existing file to force recreation
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    System.Diagnostics.Debug.WriteLine("Deleted existing snippets.json");
                }
                
                // Clear in-memory snippets
                _snippets.Clear();
                
                // Reload with sample data
                await LoadSnippetsAsync();
                
                System.Diagnostics.Debug.WriteLine($"ForceReloadSampleData: Loaded {_snippets.Count} sample snippets");
                foreach (var snippet in _snippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ForceReloadSampleData: {ex.Message}");
            }
        }

        public async Task ForceDeleteAndReload()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ForceDeleteAndReload: Starting complete data reset");
                
                // Delete existing file
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    System.Diagnostics.Debug.WriteLine("Deleted existing snippets.json");
                }
                
                // Clear in-memory snippets
                _snippets.Clear();
                
                // Create fresh sample data
                _snippets = CreateSampleSnippets();
                
                // Save to file
                await SaveSnippetsAsync();
                
                System.Diagnostics.Debug.WriteLine($"ForceDeleteAndReload: Created {_snippets.Count} fresh sample snippets");
                foreach (var snippet in _snippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ForceDeleteAndReload: {ex.Message}");
            }
        }

        public void ForceFreshSampleData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("ForceFreshSampleData: Forcing immediate use of fresh sample data");
                
                // Clear in-memory snippets
                _snippets.Clear();
                
                // Create fresh sample data immediately
                _snippets = CreateSampleSnippets();
                
                System.Diagnostics.Debug.WriteLine($"ForceFreshSampleData: Created {_snippets.Count} fresh sample snippets");
                foreach (var snippet in _snippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
                
                // Force save to file
                _ = Task.Run(async () => await SaveSnippetsAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ForceFreshSampleData: {ex.Message}");
            }
        }

        private async Task LoadSnippetsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"LoadSnippetsAsync: Checking file at {_filePath}");
                
                if (File.Exists(_filePath))
                {
                    System.Diagnostics.Debug.WriteLine("LoadSnippetsAsync: Loading existing snippets.json");
                    var json = await File.ReadAllTextAsync(_filePath);
                    _snippets = JsonConvert.DeserializeObject<List<Snippet>>(json) ?? new List<Snippet>();
                    System.Diagnostics.Debug.WriteLine($"LoadSnippetsAsync: Loaded {_snippets.Count} snippets from file");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LoadSnippetsAsync: File not found, creating sample snippets");
                    // Create sample snippets for first-time users
                    _snippets = CreateSampleSnippets();
                    await SaveSnippetsAsync();
                    System.Diagnostics.Debug.WriteLine($"LoadSnippetsAsync: Created and saved {_snippets.Count} sample snippets");
                }
                
                // Debug: Log all snippets
                System.Diagnostics.Debug.WriteLine($"LoadSnippetsAsync: Final snippet count: {_snippets.Count}");
                foreach (var snippet in _snippets)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {snippet.Title}: [{string.Join(", ", snippet.Tags ?? new List<string>())}]");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadSnippetsAsync: Error occurred: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("LoadSnippetsAsync: Falling back to sample snippets");
                _snippets = CreateSampleSnippets();
            }
        }

        private async Task SaveSnippetsAsync()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_snippets, Formatting.Indented);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception)
            {
                // Handle save errors
            }
        }

        private List<Snippet> CreateSampleSnippets()
        {
            return new List<Snippet>
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
        }
    }
}
