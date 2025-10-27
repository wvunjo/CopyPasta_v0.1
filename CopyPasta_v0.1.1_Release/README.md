# CopyPasta Native v0.1.1

A native Windows code snippet manager built with C# WPF and Material Design.

## 🚀 **What This Is**

CopyPasta Native is a personal code snippet manager that functions as a developer-friendly knowledge base. It allows you to store, edit, tag, and copy code snippets across various programming languages.

## ✨ **Features**

- **Add, edit, and delete code snippets**
- **Support for 60+ programming languages** (HTML, CSS, JavaScript, Python, C#, Java, and many more)
- **Tag-based organization** for easy categorization
- **Search functionality** to find snippets quickly
- **Copy-to-clipboard** with one click
- **Dark/light theme toggle** with consistent theming across all operations
- **Local JSON storage** - your data stays on your machine
- **Modern Material Design UI**
- **Real-time tag filtering** with immediate visual feedback
- **Persistent theme consistency** across all UI operations

## 🆕 **What's New in v0.1.1**

### **Bug Fixes & Improvements**
- **Fixed tag filtering system** - Now filters snippets immediately upon tag selection (not on deselection)
- **Resolved dark theme inconsistencies** - Dark mode now maintains consistency across all UI sections and operations
- **Eliminated false error dialogs** - Copy operations no longer show unnecessary error messages
- **Improved UI responsiveness** - Tag filtering and snippet operations are now immediate and smooth
- **Enhanced theme persistence** - Dark theme is maintained during filtering, CRUD operations, and data refresh

### **Technical Improvements**
- **Replaced data binding approach** with direct UI manipulation for better performance
- **Implemented new filtering system** (`ApplyNewFilteringSystem`) for reliable tag-based filtering
- **Enhanced theme application** to cover all UI elements consistently
- **Streamlined copy functionality** for clean, error-free operations

## 🛠 **Tech Stack**

- **Framework**: .NET 8.0 WPF
- **UI**: Material Design for WPF
- **Storage**: Local JSON files (stored in `%APPDATA%\CopyPasta\`)
- **Architecture**: Direct UI manipulation with theme consistency management

## 📁 **Project Structure**

```
CopyPastaNative/
├── Models/
│   └── Snippet.cs              # Data model for code snippets
├── Services/
│   └── SnippetService.cs       # Data persistence and CRUD operations
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

## 💡 **Usage**

1. **Add Snippet**: Click the "+" button to create a new code snippet
2. **Edit**: Click the edit button on any snippet to modify it
3. **Copy**: Click the copy button to copy code to clipboard
4. **Search**: Use the search bar to find snippets by title, language, or tags
5. **Filter**: Click on tags to filter snippets (immediate filtering)
6. **Theme**: Toggle between light and dark themes (consistent across all operations)
7. **Clear Filters**: Reset to show all snippets

## 🔧 **Development**

### **Building from Source**
1. Clone this repository
2. Open `CopyPastaNative.csproj` in Visual Studio 2022
3. Build and run the project

### **Dependencies**
- `MaterialDesignThemes.Wpf` - UI components and theming
- `MaterialDesignColors` - Color schemes
- `Newtonsoft.Json` - JSON serialization

## 📝 **Data Format**

Snippets are stored with the following structure:
```json
{
  "id": "unique-guid",
  "title": "Snippet Title",
  "language": "csharp",
  "tags": ["tag1", "tag2"],
  "code": "// Your code here",
  "createdAt": "2024-01-01T00:00:00",
  "updatedAt": "2024-01-01T00:00:00"
}
```

## 🌟 **What's Next (Future Versions)**

- Syntax highlighting for code editor
- Export/import functionality
- Keyboard shortcuts
- Multiple snippet collections
- Cloud sync (optional)
- Plugin system

## 📄 **License**

This project is open source and available under the MIT License.

## 🤝 **Contributing**

This is currently a personal project, but contributions are welcome! Feel free to submit issues or pull requests.

---

**Version**: 0.1.1  
**Release Date**: August 2025  
**Status**: Stable Release - Major Bug Fixes & Theme Improvements Complete

