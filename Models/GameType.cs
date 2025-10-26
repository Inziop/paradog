using System.Collections.Generic;

namespace ParadoxTranslator.Models;

public enum GameType
{
    Generic,
    Stellaris,
    HeartsOfIronIV,
    EuropaUniversalisIV,
    CrusaderKings3,
    Victoria3
}

public class GameConfig
{
    public GameType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LocalizationFolder { get; set; } = "localisation"; // Default
    public Dictionary<string, string> LanguageKeys { get; set; } = new();
    public string IconEmoji { get; set; } = "🎮";
    
    // For games that don't support custom languages (like Vietnamese in Victoria 3),
    // we need to override an existing language instead of creating new language folders
    public bool UseOverrideMode { get; set; } = false;
    public string OverrideLanguage { get; set; } = "english"; // Which language to override

    public static Dictionary<GameType, GameConfig> GetAllConfigs()
    {
        var defaultLanguageKeys = new Dictionary<string, string>
        {
            { "en", "english" },
            { "fr", "french" },
            { "de", "german" },
            { "es", "spanish" },
            { "pl", "polish" },
            { "pt", "portuguese" },
            { "ru", "russian" },
            { "zh", "chinese" },
            { "kr", "korean" },
            { "jp", "japanese" }
        };

        return new Dictionary<GameType, GameConfig>
        {
            {
                GameType.Generic,
                new GameConfig
                {
                    Type = GameType.Generic,
                    Name = "generic",
                    DisplayName = "Generic Paradox Game",
                    LocalizationFolder = "localisation",
                    LanguageKeys = defaultLanguageKeys,
                    IconEmoji = "🎮"
                }
            },
            {
                GameType.Stellaris,
                new GameConfig
                {
                    Type = GameType.Stellaris,
                    Name = "stellaris",
                    DisplayName = "Stellaris",
                    LocalizationFolder = "localisation",
                    LanguageKeys = new Dictionary<string, string>(defaultLanguageKeys)
                    {
                        ["pt"] = "braz_por",
                        ["zh"] = "simp_chinese"
                    },
                    IconEmoji = "🌌"
                }
            },
            {
                GameType.HeartsOfIronIV,
                new GameConfig
                {
                    Type = GameType.HeartsOfIronIV,
                    Name = "hoi4",
                    DisplayName = "Hearts of Iron IV",
                    LocalizationFolder = "localisation",
                    LanguageKeys = defaultLanguageKeys,
                    IconEmoji = "⚔️"
                }
            },
            {
                GameType.EuropaUniversalisIV,
                new GameConfig
                {
                    Type = GameType.EuropaUniversalisIV,
                    Name = "eu4",
                    DisplayName = "Europa Universalis IV",
                    LocalizationFolder = "localisation",
                    LanguageKeys = defaultLanguageKeys,
                    IconEmoji = "🗺️"
                }
            },
            {
                GameType.CrusaderKings3,
                new GameConfig
                {
                    Type = GameType.CrusaderKings3,
                    Name = "ck3",
                    DisplayName = "Crusader Kings III",
                    LocalizationFolder = "localization", // Note: 'z' instead of 's'
                    LanguageKeys = defaultLanguageKeys,
                    IconEmoji = "👑"
                }
            },
            {
                GameType.Victoria3,
                new GameConfig
                {
                    Type = GameType.Victoria3,
                    Name = "vic3",
                    DisplayName = "Victoria 3",
                    LocalizationFolder = "localization", // Victoria 3 uses 'z' spelling
                    LanguageKeys = defaultLanguageKeys,
                    IconEmoji = "🏭",
                    UseOverrideMode = true, // Victoria 3 doesn't support custom languages
                    OverrideLanguage = "english" // Override English files with Vietnamese content
                }
            }
        };
    }
}
