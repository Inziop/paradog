using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ParadoxTranslator.Services;

/// <summary>
/// Localization service for multi-language support (English/Vietnamese)
/// </summary>
public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    private string _currentLanguage = "en";

    public static LocalizationService Instance => _instance ??= new LocalizationService();

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["en"] = new Dictionary<string, string>
        {
            // Main Window
            ["MenuFile"] = "File",
            ["MenuOpen"] = "Open",
            ["MenuSave"] = "Save",
            ["MenuSaveAll"] = "Save All",
            ["MenuExport"] = "Export",
            ["MenuSettings"] = "Settings",
            ["MenuStatistics"] = "Statistics",
            ["MenuHelp"] = "Help",
            ["MenuAbout"] = "About",
            ["SearchPlaceholder"] = "Search files...",
            ["SelectAll"] = "Select All",
            ["DeselectAll"] = "Deselect All",
            ["BatchTranslate"] = "Batch Translate",
            ["TranslateSelected"] = "Translate Selected",
            ["OpenFile"] = "Open File",
            ["SwitchProject"] = "Switch",
            ["FromLanguage"] = "From:",
            ["ToLanguage"] = "To:",
            ["EnableAICheckbox"] = "Enable AI",
            ["ColumnKey"] = "KEY",
            ["ColumnSource"] = "SOURCE TEXT",
            ["ColumnTranslation"] = "TRANSLATION",
            ["ColumnActions"] = "ACTIONS",
            ["ColumnStatus"] = "STATUS",
            ["StatusReady"] = "Ready",
            ["StatusTranslated"] = "Translated",
            ["StatusUntranslated"] = "Untranslated",
            ["StatusError"] = "Error",
            ["CopySource"] = "Copy Source",
            ["Translate"] = "Translate",
            ["CopyKey"] = "Copy Key",
            ["CopySourceText"] = "Copy Source Text",
            ["CopyTranslation"] = "Copy Translation",
            ["ClearTranslation"] = "Clear Translation",
            ["CopySourceToTranslation"] = "Copy Source to Translation",
            ["TranslateWithAI"] = "Translate with AI",
            ["FilesLoaded"] = "Files loaded",
            ["TranslationProgress"] = "Translation progress",
            
            // Settings Window
            ["Settings"] = "Settings",
            ["SettingsTitle"] = "Configure your translation preferences",
            ["TranslationEngine"] = "Translation Engine",
            ["SelectEngine"] = "Select your preferred translation service:",
            ["EnableAI"] = "Enable AI translation",
            ["AINote"] = "If enabled, translations use the configured AI engine and API key",
            ["GoogleTranslate"] = "Google Translate",
            ["GoogleAIStudio"] = "Google AI Studio (Gemini)",
            ["DeepL"] = "DeepL",
            ["OpenAIGPT"] = "OpenAI GPT",
            ["EngineNote"] = "Note: Each service requires its own API key. Google AI Studio (Gemini) provides more contextual translations.",
            ["APIConfiguration"] = "API Configuration",
            ["APIKeys"] = "API Keys (Optional - for premium features):",
            ["GoogleAPIKey"] = "Google API Key:",
            ["DeepLAPIKey"] = "DeepL API Key:",
            ["GeminiAPIKey"] = "Gemini API Key:",
            ["TestButton"] = "Test",
            ["GetKeyButton"] = "Get Key",
            ["AdvancedGemini"] = "Advanced: Gemini Endpoint",
            ["APIEndpoint"] = "API Endpoint:",
            ["TranslationSettings"] = "Translation Settings",
            ["OverwriteExisting"] = "Overwrite existing translations",
            ["ValidatePlaceholders"] = "Validate placeholders automatically",
            ["CreateBackup"] = "Create backup before saving",
            ["MaxConcurrent"] = "Max concurrent requests:",
            ["InterfaceSettings"] = "Interface Settings",
            ["Theme"] = "Theme:",
            ["ThemeDark"] = "Dark",
            ["ThemeLight"] = "Light",
            ["ThemeAuto"] = "Auto",
            ["Language"] = "Language:",
            ["LanguageEnglish"] = "English",
            ["LanguageVietnamese"] = "Tiếng Việt",
            ["ShowAnimations"] = "Show progress animations",
            ["AutoSave"] = "Auto-save every 5 minutes",
            ["AdvancedSettings"] = "Advanced Settings",
            ["TranslationQuality"] = "Translation Quality:",
            ["QualityFast"] = "Fast (Lower Quality)",
            ["QualityBalanced"] = "Balanced",
            ["QualityHigh"] = "High Quality (Slower)",
            ["EnableLogging"] = "Enable detailed logging",
            ["ShowDebugInfo"] = "Show debug information",
            ["ResetDefaults"] = "Reset to Defaults",
            ["Cancel"] = "Cancel",
            ["SaveSettings"] = "Save Settings",
            
            // Statistics Window
            ["Statistics"] = "Statistics",
            ["StatisticsSubtitle"] = "View your translation progress and statistics",
            ["OverallProgress"] = "Overall Progress",
            ["TotalFiles"] = "Total Files:",
            ["TotalEntries"] = "Total Entries:",
            ["TranslatedEntries"] = "Translated:",
            ["OverallProgressLabel"] = "Overall Progress:",
            ["FileStatistics"] = "File Statistics",
            ["FileName"] = "File Name",
            ["Total"] = "Total",
            ["Translated"] = "Translated",
            ["Progress"] = "Progress",
            ["TranslationQualityStats"] = "Translation Quality",
            ["Successful"] = "Successful:",
            ["Failed"] = "Failed:",
            ["TimeStatistics"] = "Time Statistics",
            ["SessionStart"] = "Session Start:",
            ["TimeElapsed"] = "Time Elapsed:",
            ["AvgPerEntry"] = "Avg. per Entry:",
            ["ExportReport"] = "Export Report",
            ["Refresh"] = "Refresh",
            ["Close"] = "Close",
            
            // Welcome Window
            ["Welcome"] = "Help & Welcome",
            ["WelcomeTitle"] = "Welcome to Paradox Translator",
            ["WelcomeSubtitle"] = "Professional localization tool for Paradox Interactive games",
            ["QuickStart"] = "Quick Start Guide",
            ["Step1Title"] = "Open Files",
            ["Step1Text"] = "Click the Open button to load YAML localization files from your game mod",
            ["Step2Title"] = "Configure Settings",
            ["Step2Text"] = "Set up your preferred translation engine and API keys in Settings",
            ["Step3Title"] = "Start Translating",
            ["Step3Text"] = "Select entries and use the Translate button or batch translate feature",
            ["BestPractices"] = "Best Practices",
            ["Practice1"] = "Always validate placeholders to avoid breaking game text",
            ["Practice2"] = "Use high-quality translation engines for important text",
            ["Practice3"] = "Create backups before making major changes",
            ["Practice4"] = "Review AI translations for context and accuracy",
            ["GetStarted"] = "Get Started",
            ["ViewDocumentation"] = "View Documentation",
            
            // About Window
            ["About"] = "About",
            ["AboutTitle"] = "About Paradox Translator",
            ["AboutVersion"] = "Version 1.0.0",
            ["AboutSubtitle"] = "Professional localization tool for Paradox games",
            ["AboutDescription"] = "A powerful WPF application designed specifically for translating Paradox Interactive game localization files. Built with modern technology and user-friendly interface.",
            ["GitHubRepository"] = "GitHub Repository",
            ["Version"] = "Version",
            ["Copyright"] = "Copyright",
            ["License"] = "License",
            
            // Confirm Dialog
            ["Confirm"] = "Confirm",
            ["ConfirmSwitch"] = "Do you want to switch to another project? Any unsaved changes will be lost.",
            ["ConfirmClose"] = "Do you want to close the application? Any unsaved changes will be lost.",
            
            // Messages
            ["Success"] = "Success",
            ["Error"] = "Error",
            ["Warning"] = "Warning",
            ["Information"] = "Information",
            ["SaveSuccessful"] = "Files saved successfully",
            ["SaveFailed"] = "Failed to save files",
            ["TranslationSuccessful"] = "Translation completed successfully",
            ["TranslationFailed"] = "Translation failed",
        },
        ["vi"] = new Dictionary<string, string>
        {
            // Main Window
            ["MenuFile"] = "Tệp",
            ["MenuOpen"] = "Mở",
            ["MenuSave"] = "Lưu",
            ["MenuSaveAll"] = "Lưu tất cả",
            ["MenuExport"] = "Xuất",
            ["MenuSettings"] = "Cài đặt",
            ["MenuStatistics"] = "Thống kê",
            ["MenuHelp"] = "Trợ giúp",
            ["MenuAbout"] = "Giới thiệu",
            ["SearchPlaceholder"] = "Tìm kiếm tệp...",
            ["SelectAll"] = "Chọn tất cả",
            ["DeselectAll"] = "Bỏ chọn tất cả",
            ["BatchTranslate"] = "Dịch hàng loạt",
            ["TranslateSelected"] = "Dịch đã chọn",
            ["OpenFile"] = "Mở tệp",
            ["SwitchProject"] = "Chuyển",
            ["FromLanguage"] = "Từ:",
            ["ToLanguage"] = "Đến:",
            ["EnableAICheckbox"] = "Bật AI",
            ["ColumnKey"] = "KHÓA",
            ["ColumnSource"] = "VĂN BẢN GỐC",
            ["ColumnTranslation"] = "BẢN DỊCH",
            ["ColumnActions"] = "THAO TÁC",
            ["ColumnStatus"] = "TRẠNG THÁI",
            ["StatusReady"] = "Sẵn sàng",
            ["StatusTranslated"] = "Đã dịch",
            ["StatusUntranslated"] = "Chưa dịch",
            ["StatusError"] = "Lỗi",
            ["CopySource"] = "Sao chép gốc",
            ["Translate"] = "Dịch",
            ["CopyKey"] = "Sao chép khóa",
            ["CopySourceText"] = "Sao chép văn bản gốc",
            ["CopyTranslation"] = "Sao chép bản dịch",
            ["ClearTranslation"] = "Xóa bản dịch",
            ["CopySourceToTranslation"] = "Sao chép gốc sang bản dịch",
            ["TranslateWithAI"] = "Dịch bằng AI",
            ["FilesLoaded"] = "Tệp đã tải",
            ["TranslationProgress"] = "Tiến độ dịch",
            
            // Settings Window
            ["Settings"] = "Cài đặt",
            ["SettingsTitle"] = "Cấu hình tùy chọn dịch thuật của bạn",
            ["TranslationEngine"] = "Công cụ dịch",
            ["SelectEngine"] = "Chọn dịch vụ dịch thuật ưa thích:",
            ["EnableAI"] = "Bật dịch AI",
            ["AINote"] = "Nếu bật, bản dịch sẽ sử dụng công cụ AI và khóa API đã cấu hình",
            ["GoogleTranslate"] = "Google Dịch",
            ["GoogleAIStudio"] = "Google AI Studio (Gemini)",
            ["DeepL"] = "DeepL",
            ["OpenAIGPT"] = "OpenAI GPT",
            ["EngineNote"] = "Lưu ý: Mỗi dịch vụ yêu cầu khóa API riêng. Google AI Studio (Gemini) cung cấp bản dịch theo ngữ cảnh tốt hơn.",
            ["APIConfiguration"] = "Cấu hình API",
            ["APIKeys"] = "Khóa API (Tùy chọn - cho tính năng cao cấp):",
            ["GoogleAPIKey"] = "Khóa API Google:",
            ["DeepLAPIKey"] = "Khóa API DeepL:",
            ["GeminiAPIKey"] = "Khóa API Gemini:",
            ["TestButton"] = "Kiểm tra",
            ["GetKeyButton"] = "Lấy khóa",
            ["AdvancedGemini"] = "Nâng cao: Endpoint Gemini",
            ["APIEndpoint"] = "Endpoint API:",
            ["TranslationSettings"] = "Cài đặt dịch thuật",
            ["OverwriteExisting"] = "Ghi đè bản dịch hiện có",
            ["ValidatePlaceholders"] = "Tự động kiểm tra placeholder",
            ["CreateBackup"] = "Tạo bản sao lưu trước khi lưu",
            ["MaxConcurrent"] = "Số yêu cầu đồng thời tối đa:",
            ["InterfaceSettings"] = "Cài đặt giao diện",
            ["Theme"] = "Giao diện:",
            ["ThemeDark"] = "Tối",
            ["ThemeLight"] = "Sáng",
            ["ThemeAuto"] = "Tự động",
            ["Language"] = "Ngôn ngữ:",
            ["LanguageEnglish"] = "English",
            ["LanguageVietnamese"] = "Tiếng Việt",
            ["ShowAnimations"] = "Hiển thị hiệu ứng tiến độ",
            ["AutoSave"] = "Tự động lưu mỗi 5 phút",
            ["AdvancedSettings"] = "Cài đặt nâng cao",
            ["TranslationQuality"] = "Chất lượng dịch:",
            ["QualityFast"] = "Nhanh (Chất lượng thấp hơn)",
            ["QualityBalanced"] = "Cân bằng",
            ["QualityHigh"] = "Chất lượng cao (Chậm hơn)",
            ["EnableLogging"] = "Bật ghi log chi tiết",
            ["ShowDebugInfo"] = "Hiển thị thông tin debug",
            ["ResetDefaults"] = "Đặt lại mặc định",
            ["Cancel"] = "Hủy",
            ["SaveSettings"] = "Lưu cài đặt",
            
            // Statistics Window
            ["Statistics"] = "Thống kê",
            ["StatisticsSubtitle"] = "Xem tiến độ và thống kê dịch thuật của bạn",
            ["OverallProgress"] = "Tiến độ tổng thể",
            ["TotalFiles"] = "Tổng số tệp:",
            ["TotalEntries"] = "Tổng số mục:",
            ["TranslatedEntries"] = "Đã dịch:",
            ["OverallProgressLabel"] = "Tiến độ tổng thể:",
            ["FileStatistics"] = "Thống kê tệp",
            ["FileName"] = "Tên tệp",
            ["Total"] = "Tổng",
            ["Translated"] = "Đã dịch",
            ["Progress"] = "Tiến độ",
            ["TranslationQualityStats"] = "Chất lượng dịch",
            ["Successful"] = "Thành công:",
            ["Failed"] = "Thất bại:",
            ["TimeStatistics"] = "Thống kê thời gian",
            ["SessionStart"] = "Bắt đầu phiên:",
            ["TimeElapsed"] = "Thời gian trôi qua:",
            ["AvgPerEntry"] = "TB mỗi mục:",
            ["ExportReport"] = "Xuất báo cáo",
            ["Refresh"] = "Làm mới",
            ["Close"] = "Đóng",
            
            // Welcome Window
            ["Welcome"] = "Trợ giúp & Chào mừng",
            ["WelcomeTitle"] = "Chào mừng đến với Paradox Translator",
            ["WelcomeSubtitle"] = "Công cụ bản địa hóa chuyên nghiệp cho game Paradox Interactive",
            ["QuickStart"] = "Hướng dẫn nhanh",
            ["Step1Title"] = "Mở tệp",
            ["Step1Text"] = "Nhấn nút Mở để tải tệp YAML bản địa hóa từ mod game của bạn",
            ["Step2Title"] = "Cấu hình cài đặt",
            ["Step2Text"] = "Thiết lập công cụ dịch ưa thích và khóa API trong Cài đặt",
            ["Step3Title"] = "Bắt đầu dịch",
            ["Step3Text"] = "Chọn mục và dùng nút Dịch hoặc tính năng dịch hàng loạt",
            ["BestPractices"] = "Thực hành tốt nhất",
            ["Practice1"] = "Luôn kiểm tra placeholder để tránh làm hỏng văn bản game",
            ["Practice2"] = "Dùng công cụ dịch chất lượng cao cho văn bản quan trọng",
            ["Practice3"] = "Tạo bản sao lưu trước khi thực hiện thay đổi lớn",
            ["Practice4"] = "Xem lại bản dịch AI về ngữ cảnh và độ chính xác",
            ["GetStarted"] = "Bắt đầu",
            ["ViewDocumentation"] = "Xem tài liệu",
            
            // About Window
            ["About"] = "Giới thiệu",
            ["AboutTitle"] = "Giới thiệu Paradox Translator",
            ["AboutVersion"] = "Phiên bản 1.0.0",
            ["AboutSubtitle"] = "Công cụ địa phương hóa chuyên nghiệp cho game Paradox",
            ["AboutDescription"] = "Ứng dụng WPF mạnh mẽ được thiết kế đặc biệt để dịch các tệp địa phương hóa game Paradox Interactive. Được xây dựng với công nghệ hiện đại và giao diện thân thiện với người dùng.",
            ["GitHubRepository"] = "Kho GitHub",
            ["Version"] = "Phiên bản",
            ["Copyright"] = "Bản quyền",
            ["License"] = "Giấy phép",
            
            // Confirm Dialog
            ["Confirm"] = "Xác nhận",
            ["ConfirmSwitch"] = "Bạn có muốn chuyển sang dự án khác? Mọi thay đổi chưa lưu sẽ bị mất.",
            ["ConfirmClose"] = "Bạn có muốn đóng ứng dụng? Mọi thay đổi chưa lưu sẽ bị mất.",
            
            // Messages
            ["Success"] = "Thành công",
            ["Error"] = "Lỗi",
            ["Warning"] = "Cảnh báo",
            ["Information"] = "Thông tin",
            ["SaveSuccessful"] = "Lưu tệp thành công",
            ["SaveFailed"] = "Lưu tệp thất bại",
            ["TranslationSuccessful"] = "Dịch hoàn tất thành công",
            ["TranslationFailed"] = "Dịch thất bại",
        }
    };

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged();
                // Notify all string properties changed
                OnPropertyChanged(string.Empty);
            }
        }
    }

    public string this[string key]
    {
        get
        {
            if (_translations.TryGetValue(_currentLanguage, out var langDict) &&
                langDict.TryGetValue(key, out var value))
            {
                return value;
            }
            // Fallback to English
            if (_currentLanguage != "en" && _translations["en"].TryGetValue(key, out var fallback))
            {
                return fallback;
            }
            return $"[{key}]"; // Return key if not found
        }
    }

    public void LoadLanguage()
    {
        var config = SettingsService.LoadConfig();
        CurrentLanguage = config.AppLanguage;
    }

    public void SaveLanguage(string language)
    {
        var config = SettingsService.LoadConfig();
        config.AppLanguage = language;
        SettingsService.SaveConfig(config);
        CurrentLanguage = language;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
