using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

/// <summary>
/// Parser for Paradox localization files
/// </summary>
public class ParadoxParser
{
    // Fixed regex: Removed catastrophic backtracking pattern, added timeout
    private static readonly Regex KeyValueRegex = new(
        @"^([^#\s]+)\s*[:=]\s*(\d+)?\s*""([^""]*)""", 
        RegexOptions.Compiled, 
        TimeSpan.FromSeconds(1));
    private static readonly Regex CommentRegex = new(@"^\s*#", RegexOptions.Compiled);
    private static readonly Regex HeaderRegex = new(@"^l_\w+:", RegexOptions.Compiled);

    /// <summary>
    /// Parse a localization file from stream
    /// </summary>
    public static List<LocalizationEntry> Parse(TextReader reader)
    {
        var entries = new List<LocalizationEntry>();
        string? line;
        int lineNumber = 0;
        string? currentHeader = null;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            var trimmedLine = line.Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Check for header (l_english:, l_french:, etc.)
            if (HeaderRegex.IsMatch(trimmedLine))
            {
                currentHeader = trimmedLine;
                continue;
            }

            // Skip comments
            if (CommentRegex.IsMatch(trimmedLine))
            {
                continue;
            }

            // Parse key-value pairs
            var match = KeyValueRegex.Match(trimmedLine);
            if (match.Success)
            {
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[3].Value;

                // Handle escaped quotes in the value
                value = UnescapeQuotes(value);

                var entry = new LocalizationEntry
                {
                    Key = key,
                    SourceText = value,
                    TranslatedText = string.Empty,
                    RawLineBefore = line,
                    RawLineAfter = string.Empty
                };

                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <summary>
    /// Parse a localization file from file path
    /// </summary>
    public static async Task<List<LocalizationEntry>> ParseFileAsync(string filePath)
    {
        // Use true async I/O to avoid blocking UI thread
        var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        using var reader = new StringReader(content);
        return Parse(reader);
    }

    /// <summary>
    /// Build localization file from entries
    /// </summary>
    public static string BuildLocalization(IEnumerable<LocalizationEntry> entries, string header = "l_english:")
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine();

        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.TranslatedText))
            {
                sb.AppendLine($"{entry.Key}:0 \"{EscapeQuotes(entry.TranslatedText)}\"");
            }
            else
            {
                sb.AppendLine($"{entry.Key}:0 \"{EscapeQuotes(entry.SourceText)}\"");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Save localization to file
    /// </summary>
    public static async Task SaveLocalizationAsync(IEnumerable<LocalizationEntry> entries, string filePath, string header = "l_english:")
    {
        var content = BuildLocalization(entries, header);
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
    }

    private static string UnescapeQuotes(string text)
    {
        return text.Replace("\\\"", "\"");
    }

    private static string EscapeQuotes(string text)
    {
        return text.Replace("\"", "\\\"");
    }
}
