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
                s.Title.ToLowerInvariant().Contains(searchTerm) ||
                s.Code.ToLowerInvariant().Contains(searchTerm) ||
                s.Tags.Any(tag => tag.ToLowerInvariant().Contains(searchTerm))
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
                tags.All(tag => s.Tags.Contains(tag))
            ).ToList();
        }

        public List<string> GetAllTags()
        {
            return _snippets
                .SelectMany(s => s.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        private async Task LoadSnippetsAsync()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = await File.ReadAllTextAsync(_filePath);
                    _snippets = JsonConvert.DeserializeObject<List<Snippet>>(json) ?? new List<Snippet>();
                }
                else
                {
                    // Create sample snippets for first-time users
                    _snippets = CreateSampleSnippets();
                    await SaveSnippetsAsync();
                }
            }
            catch (Exception)
            {
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
                )
            };
        }
    }
}
