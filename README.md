## ✨ Features

### 🔧 Core Functionality
- **Batch Translation**: Translate multiple YAML files simultaneously
- **Multiple Translation Engines**: Google Translate, DeepL, Microsoft Translator, OpenAI GPT
- **Smart File Detection**: Automatically detects and loads all localization files in a folder
- **Progress Tracking**: Real-time progress bars and statistics
- **Backup System**: Automatic backup creation before saving

### 🎨 User Interface
- **Modern Dark Theme**: Beautiful, professional interface
- **Responsive Design**: Adapts to different window sizes
- **Search & Filter**: Quick file search and filtering
- **Statistics Dashboard**: Comprehensive translation metrics
- **Settings Panel**: Customizable translation options

### 🚀 Advanced Features
- **Placeholder Validation**: Ensures game variables are preserved
- **Export Options**: Export translations to JSON, CSV, or Excel
- **Quality Control**: Translation quality settings
- **Auto-save**: Optional automatic saving
- **Debug Mode**: Detailed logging and debugging information

## 🛠️ Installation

### Prerequisites
- .NET 8.0 Runtime
- Windows 10/11

### Download
1. Download the latest release from [Releases](https://github.com/yourusername/ParadoxTranslator/releases)
2. Extract the ZIP file
3. Run `ParadoxTranslator.exe`

### Build from Source
```bash
git clone https://github.com/yourusername/ParadoxTranslator.git
cd ParadoxTranslator
dotnet restore
dotnet build
dotnet run
```

## 📖 Usage

### 1. Open Localization Folder
- Click "📁 Open Folder" to select your game's localization directory
- The app will automatically scan for `.yml`, `.yaml`, and `.txt` files
- Files will appear in the left panel with progress indicators

### 2. Configure Settings
- Click "⚙️ Settings" to configure:
  - Translation engine (Google, DeepL, Microsoft, OpenAI)
  - API keys for premium services
  - Translation quality settings
  - UI preferences

### 3. Translate Files
- Select a file from the left panel
- Choose source and target languages
- Click "🔄 Translate All" for automatic translation
- Or manually edit translations in the Target column

### 4. Save and Export
- Click "💾 Save" to save changes to the original files
- Use "💾 Export" to export translations to other formats
- View "📊 Statistics" for progress tracking

## 🎯 Supported Games

- **Victoria 3**
- **Europa Universalis IV**
- **Crusader Kings III**
- **Hearts of Iron IV**
- **Stellaris**
- **Imperator: Rome**
- And other Paradox games with YAML localization

## 🔧 Configuration

### Translation Engines

#### Google Translate (Free)
- No API key required
- Good for basic translations
- Rate limited

#### DeepL (Premium)
- Requires API key
- High-quality translations
- Supports multiple languages

#### Microsoft Translator (Premium)
- Requires API key
- Good for technical content
- Enterprise-grade reliability

#### OpenAI GPT (Premium)
- Requires API key
- Context-aware translations
- Best for complex text

### Settings Options

| Setting | Description | Default |
|---------|-------------|---------|
| Translation Engine | Choose your preferred service | Google |
| Overwrite Existing | Replace existing translations | Yes |
| Validate Placeholders | Check for game variables | Yes |
| Create Backup | Backup before saving | Yes |
| Max Concurrent Requests | Number of simultaneous translations | 3 |
| Auto-save | Save automatically every 5 minutes | No |

## 📊 File Structure

```
ParadoxTranslator/
├── Models/                 # Data models
├── Services/               # Translation and file services
├── ViewModels/            # MVVM view models
├── Utils/                  # Utility classes
├── Views/                  # XAML windows
└── README.md
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Paradox Interactive for creating amazing games
- Translation service providers (Google, DeepL, Microsoft, OpenAI)
- The WPF and .NET community
- Contributors and testers

## 🐛 Bug Reports & Feature Requests

Please use the [Issues](https://github.com/yourusername/ParadoxTranslator/issues) section to:
- Report bugs
- Request new features
- Ask questions
- Share feedback


## 💡 Tips & Tricks

### For Game Modders
- Always backup your original files
- Test translations in-game before publishing
- Use placeholder validation to avoid breaking game variables
- Consider using high-quality translation engines for important text

### For Translators
- Use the statistics panel to track your progress
- Export your work regularly to avoid data loss
- Enable auto-save for long translation sessions
- Use search to quickly find specific entries


