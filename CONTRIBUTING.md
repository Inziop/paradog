# Contributing to Paradox Translator

Thank you for your interest in contributing to Paradox Translator! This document provides guidelines and information for contributors.

## 🤝 How to Contribute

### Reporting Bugs
- Use the [Issues](https://github.com/yourusername/ParadoxTranslator/issues) section
- Include detailed steps to reproduce the bug
- Provide system information (OS, .NET version, etc.)
- Include error messages and screenshots if applicable

### Suggesting Features
- Check existing issues first to avoid duplicates
- Provide a clear description of the feature
- Explain the use case and benefits
- Consider implementation complexity

### Code Contributions
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Test thoroughly
5. Commit with clear messages
6. Push to your fork
7. Create a Pull Request

## 🛠️ Development Setup

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- Git

### Getting Started
```bash
# Clone your fork
git clone https://github.com/yourusername/ParadoxTranslator.git
cd ParadoxTranslator

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run
```

### Project Structure
```
ParadoxTranslator/
├── Models/                 # Data models and entities
├── Services/              # Business logic and external services
├── ViewModels/           # MVVM view models
├── Utils/                # Utility classes and helpers
├── Views/                # XAML windows and user controls
├── README.md
├── LICENSE
└── CONTRIBUTING.md
```

## 📝 Coding Standards

### C# Guidelines
- Follow C# naming conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Use async/await for I/O operations
- Handle exceptions appropriately

### XAML Guidelines
- Use consistent indentation (4 spaces)
- Group related properties together
- Use meaningful names for controls
- Follow WPF best practices

### Code Style
```csharp
// Good
public async Task<string> TranslateTextAsync(string text, string targetLanguage)
{
    try
    {
        var result = await _translationService.TranslateAsync(text, targetLanguage);
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Translation failed for text: {Text}", text);
        throw;
    }
}

// Avoid
public string Translate(string t, string lang)
{
    return _service.Translate(t, lang);
}
```

## 🧪 Testing

### Manual Testing
- Test all major features
- Verify error handling
- Check UI responsiveness
- Test with different file types

### Test Cases
- [ ] Load localization files
- [ ] Translate entries
- [ ] Save changes
- [ ] Export functionality
- [ ] Settings configuration
- [ ] Error scenarios

## 📋 Pull Request Guidelines

### Before Submitting
- [ ] Code compiles without errors
- [ ] All tests pass
- [ ] Code follows style guidelines
- [ ] Documentation is updated
- [ ] No sensitive information included

### PR Description Template
```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Manual testing completed
- [ ] No regression issues
- [ ] Performance impact considered

## Screenshots (if applicable)
Add screenshots for UI changes

## Checklist
- [ ] Code follows project style
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No merge conflicts
```

## 🐛 Bug Fix Process

1. **Identify the Issue**
   - Reproduce the bug
   - Identify root cause
   - Check existing issues

2. **Create Fix**
   - Write minimal fix
   - Add tests if applicable
   - Update documentation

3. **Test Fix**
   - Verify bug is resolved
   - Check for regressions
   - Test edge cases

## ✨ Feature Development

1. **Plan the Feature**
   - Define requirements
   - Consider UI/UX impact
   - Plan implementation approach

2. **Implement**
   - Follow existing patterns
   - Add comprehensive error handling
   - Include user feedback

3. **Test and Document**
   - Thorough testing
   - Update documentation
   - Add examples if needed

## 🎨 UI/UX Guidelines

### Design Principles
- **Consistency**: Follow existing design patterns
- **Accessibility**: Ensure keyboard navigation works
- **Responsiveness**: Handle different window sizes
- **User Feedback**: Provide clear status updates

### Color Scheme
- Primary: #007ACC (Blue)
- Success: #28A745 (Green)
- Warning: #FFC107 (Yellow)
- Danger: #DC3545 (Red)
- Background: #1E1E1E (Dark)
- Surface: #2D2D30 (Darker)

### Typography
- Headers: Segoe UI, Bold
- Body: Segoe UI, Regular
- Code: Consolas, Regular

## 📚 Documentation

### Code Documentation
- XML comments for public APIs
- Inline comments for complex logic
- README updates for new features

### User Documentation
- Update README.md for new features
- Add screenshots for UI changes
- Include usage examples

## 🔒 Security Considerations

- Never commit API keys or secrets
- Validate all user inputs
- Use secure communication protocols
- Handle sensitive data appropriately

## 📞 Getting Help

- Check existing issues and discussions
- Ask questions in GitHub Discussions
- Join our community Discord (if available)
- Email: your-email@example.com

## 🏆 Recognition

Contributors will be:
- Listed in the README
- Mentioned in release notes
- Given appropriate credit in code

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to Paradox Translator! 🎮✨
