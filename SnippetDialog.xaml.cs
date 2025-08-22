using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CopyPastaNative.Models;

namespace CopyPastaNative
{
    public partial class SnippetDialog : Window
    {
        public Snippet Snippet { get; private set; }
        private readonly bool _isEditMode;

        private static readonly List<string> Languages = new()
        {
            // Web Technologies
            "html", "css", "javascript", "typescript", "jsx", "tsx", "php", "asp", "jsp",
            
            // Programming Languages
            "python", "java", "csharp", "cpp", "c", "go", "rust", "swift", "kotlin", "scala",
            "ruby", "perl", "lua", "r", "matlab", "dart", "elixir", "clojure", "haskell", "fsharp",
            "vb", "pascal", "fortran", "cobol", "assembly",
            
            // Scripting & Shell
            "bash", "powershell", "batch", "shell", "groovy", "vbs", "autohotkey",
            
            // Data & Configuration
            "json", "xml", "yaml", "toml", "ini", "csv", "sql", "mongodb", "graphql",
            
            // Markup & Documentation
            "markdown", "latex", "asciidoc", "rst", "wiki",
            
            // Build & Config
            "dockerfile", "docker-compose", "kubernetes", "terraform", "ansible", "makefile", "cmake",
            "gradle", "maven", "npm", "yarn",
            
            // Other
            "diff", "log", "regex", "plaintext"
        };

        public SnippetDialog()
        {
            InitializeComponent();
            _isEditMode = false;
            Snippet = new Snippet();
            InitializeDialog();
        }

        public SnippetDialog(Snippet snippet)
        {
            InitializeComponent();
            _isEditMode = true;
            Snippet = snippet;
            InitializeDialog();
            LoadSnippetData();
        }

        private void InitializeDialog()
        {
            // Populate language combo box
            LanguageComboBox.ItemsSource = Languages;
            LanguageComboBox.SelectedItem = "javascript";

            // Set dialog title
            DialogTitle.Text = _isEditMode ? "Edit Snippet" : "New Snippet";

            // Set window owner if possible
            if (Application.Current.MainWindow != null)
            {
                Owner = Application.Current.MainWindow;
            }
        }

        private void LoadSnippetData()
        {
            TitleTextBox.Text = Snippet.Title;
            LanguageComboBox.SelectedItem = Snippet.Language;
            TagsTextBox.Text = string.Join(", ", Snippet.Tags);
            CodeTextBox.Text = Snippet.Code;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                Snippet.Title = TitleTextBox.Text.Trim();
                Snippet.Language = LanguageComboBox.SelectedItem?.ToString() ?? "plaintext";
                Snippet.Tags = ParseTags(TagsTextBox.Text);
                Snippet.Code = CodeTextBox.Text;
                Snippet.UpdatedAt = DateTime.Now;

                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Please enter a title for the snippet.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CodeTextBox.Text))
            {
                MessageBox.Show("Please enter some code for the snippet.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return false;
            }

            return true;
        }

        private List<string> ParseTags(string tagsText)
        {
            if (string.IsNullOrWhiteSpace(tagsText))
                return new List<string>();

            return tagsText.Split(',')
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .ToList();
        }
    }
}
