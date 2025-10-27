# CopyPasta Native v0.2

A native Windows code snippet manager built with C# WPF and Material Design.

## 🚀 **What This Is**

CopyPasta Native is a personal code snippet manager that functions as a developer-friendly knowledge base. It allows you to store, edit, tag, and copy code snippets across various programming languages with advanced features for power users.

## ✨ **Features**

### **Core Functionality**
- **Add, edit, and delete code snippets** with full CRUD operations
- **Support for 60+ programming languages** (HTML, CSS, JavaScript, Python, C#, Java, and many more)
- **Tag-based organization** for easy categorization and filtering
- **Search functionality** to find snippets quickly by title, language, or tags
- **Copy-to-clipboard** with one click
- **Local JSON storage** - your data stays on your machine
- **Modern Material Design UI** with beautiful, intuitive interface

### **Advanced Features**
- **🔍 Search History** - Keeps track of your last 10 searches for quick access
- **🌟 Favorites System** - Mark frequently used snippets as favorites
- **📊 Statistics Panel** - View snippet count, favorites, languages, and tags
- **🔍 Duplicate Detection** - Automatically detects similar snippets before creation/editing
- **🎨 Dark/Light Themes** - Toggle between themes with full consistency
- **⌨️ Keyboard Shortcuts** - Power-user friendly keyboard navigation
- **📦 Export/Import** - Backup and restore your snippets as JSON
- **🎯 Multi-Select Mode** - Bulk operations for managing multiple snippets
- **💻 Syntax Highlighting** - Code editor with language-specific coloring (AvalonEdit)
- **🖱️ Smooth Scrolling** - Optimized mouse wheel scrolling through snippets
- **🎨 Theme-Aware Scrollbar** - Visible scrollbar that adapts to theme

### **Theme Management**
- **Dark Mode** - Easy on the eyes with consistent theming
- **Light Mode** - Clean, modern interface
- **Theme Persistence** - Maintains your preference across all operations
- **Visual Scrollbar** - Always visible, theme-aware scrollbar indicator

## 🆕 **What's New in v0.2**

### **Major Features Added**
- **✨ Syntax Highlighting** - Full code syntax highlighting using AvalonEdit for 60+ languages
- **📥 Export/Import Functionality** - Backup and restore your snippet collection
- **⭐ Favorites System** - Star snippets for quick access with dedicated filter
- **⌨️ Comprehensive Keyboard Shortcuts**:
  - `Ctrl+F` - Focus search box
  - `Ctrl+N` - New snippet
  - `Ctrl+C` - Copy snippet code
  - `Ctrl+A` - Select all (multi-select mode)
  - `Ctrl+D` - Deselect all (multi-select mode)
  - `Delete` - Delete selected snippet
  - `Enter` - Copy and move to next
  - `Esc` - Clear filters
  - `↑↓` - Navigate snippets
- **🔍 Duplicate Detection** - Intelligent similarity checking to prevent duplicates
- **📊 Statistics Panel** - Real-time snippet analytics
- **🎯 Multi-Select & Bulk Actions**:
  - Select multiple snippets
  - Bulk delete
  - Visual selection indicators
  - Selection counter
- **📚 Search History** - Last 10 searches for quick re-access
- **🖱️ Enhanced Scrolling** - Smooth, incremental mouse wheel scrolling
- **🎨 Improved Theme Support** - Visible scrollbar and consistent theming

### **Technical Improvements**
- Integrated AvalonEdit for advanced code editing
- Implemented Levenshtein distance algorithm for duplicate detection
- Enhanced theme management with scrollbar visibility
- Improved UI responsiveness for bulk operations
- Optimized scrolling performance

## ⌨️ **Keyboard Shortcuts**

| Shortcut | Action |
|----------|--------|
| `Ctrl+F` | Focus search box |
| `Ctrl+N` | Create new snippet |
| `Ctrl+C` | Copy selected snippet code |
| `Ctrl+A` | Select all snippets (multi-select mode) |
| `Ctrl+D` | Deselect all (multi-select mode) |
| `Delete` | Delete selected snippet(s) |
| `Enter` | Copy code and move to next snippet |
| `Esc` | Clear all filters |
| `↑` `↓` | Navigate through snippets |

## 🎯 **Multi-Select Mode**

Enable multi-select mode to perform bulk operations:
- **Select All** - Choose all filtered snippets at once
- **Deselect All** - Clear all selections
- **Bulk Delete** - Delete multiple snippets simultaneously
- **Visual Indicators** - Checkboxes and highlighting show selected state
- **Selection Counter** - See how many items are selected

## 🔍 **Search Features**

- **Real-time Search** - Debounced search as you type
- **Search History** - Quick access to last 10 searches
- **Tag Filtering** - Click tags to filter snippets
- **Favorites Filter** - Show only favorited snippets
- **Clear Filters** - Reset to show all snippets

## 🛠 **Tech Stack**

- **Framework**: .NET 8.0 WPF
- **UI**: Material Design for WPF
- **Code Editor**: ICSharpCode.AvalonEdit
- **Storage**: Local JSON files (stored in `%APPDATA%\CopyPasta\`)
- **Architecture**: MVVM-inspired with direct UI manipulation

## 📁 **Project Structure**

```
CopyPastaNative/
├── Models/
│   └── Snippet.cs              # Data model for code snippets
├── Services/
│   └── SnippetService.cs       # Data persistence, CRUD, and duplicate detection
├── Converters/
│   └── CountToVisibilityConverter.cs  # WPF value converter
├── MainWindow.xaml              # Main application window
├── SnippetDialog.xaml           # Add/edit snippet dialog
├── App.xaml                     # Application resources
└── CopyPastaNative.csproj      # Project file
```

## 🚀 **Getting Started**

### **Prerequisites**
- Windows 10/11
- .NET 8.0 Runtime (or Visual Studio 2022)

### **Installation**
1. Download the latest release
2. Extract to your preferred location
3. Run `CopyPastaNative.exe`

### **First Run**
- The app will create sample snippets to get you started
- Data is automatically saved to `%APPDATA%\CopyPasta\snippets.json`

## 💡 **Usage Guide**

### **Basic Operations**
1. **Add Snippet**: Click the "+ New Snippet" button or press `Ctrl+N`
2. **Edit**: Click the pencil icon on any snippet to modify it
3. **Copy**: Click the copy icon to copy code to clipboard instantly
4. **Favorite**: Click the star icon to mark as favorite
5. **Delete**: Click the trash icon or press `Delete` key

### **Search & Filter**
1. **Search**: Type in the search box to find snippets by title, language, or content
2. **Tag Filter**: Click on tag buttons to filter by category
3. **Favorites Only**: Check "Show Favorites Only" to see starred snippets
4. **History**: Your last 10 searches appear in the Recent Searches panel**

### **Multi-Select Mode**
1. Enable "Multi-Select Mode" checkbox
2. Left-click individual snippets to toggle selection
3. Use `Ctrl+A` to select all, `Ctrl+D` to deselect all
4. Use bulk delete to remove multiple snippets at once

### **Export/Import**
1. **Export**: Click "Export Snippets" to save your collection as JSON
2. **Import**: Click "Import Snippets" to load a backup (with merge option)

### **Theme Toggle**
- Click the moon/sun icon to switch between dark and light themes
- Theme preference is maintained across all operations

## 📝 **Data Format**

Snippets are stored with the following structure:
```json
{
  "id": "unique-guid",
  "title": "Snippet Title",
  "language": "csharp",
  "tags": ["tag1", "tag2"],
  "code": "// Your code here",
  "isFavorite": false,
  "createdAt": "2024-01-01T00:00:00",
  "updatedAt": "2024-01-01T00:00:00"
}
```

## 🔄 **Version History**

### **v0.2** (Current)
- Added syntax highlighting with AvalonEdit
- Implemented export/import functionality
- Added favorites system with filtering
- Comprehensive keyboard shortcuts
- Duplicate detection algorithm
- Statistics panel with real-time analytics
- Multi-select and bulk operations
- Search history (last 10 searches)
- Enhanced scrolling with mouse wheel
- Visible, theme-aware scrollbar

### **v0.1.1**
- Fixed tag filtering system
- Resolved dark theme inconsistencies
- Eliminated false error dialogs
- Improved UI responsiveness
- Enhanced theme persistence

## 🌟 **What's Next (Future Versions)**

- Plugin system for extensions
- Cloud sync option
- Multiple snippet collections/folders
- Advanced search with regular expressions
- Snippet templates
- Command-line interface
- Portable version

## 📄 **License**

This project is open source and available under the MIT License.

## 🤝 **Contributing**

This is currently a personal project, but contributions are welcome! Feel free to submit issues or pull requests.

---

**Version**: 0.2  
**Release Date**: October 2025  
**Status**: Stable Release - Feature Complete with Advanced Functionality
