using System.Text.RegularExpressions;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

/// <summary>
/// Validates placeholders in translation text
/// </summary>
public class PlaceholderValidator
{
    private static readonly Regex PlaceholderRegex = new(@"\{(\d+)\}|%[sd]|\$[A-Z_]+\[[^\]]+\]|\[Root\.GetName\]|<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex UnescapedQuotesRegex = new(@"(?<!\\)""", RegexOptions.Compiled);

    /// <summary>
    /// Validate placeholders between source and target text
    /// </summary>
    public static PlaceholderValidationResult ValidatePlaceholders(string source, string target)
    {
        var result = new PlaceholderValidationResult();

        if (string.IsNullOrWhiteSpace(target))
        {
            result.Warnings.Add("Target text is empty");
            return result;
        }

        // Extract placeholders from source and target
        var sourcePlaceholders = ExtractPlaceholders(source);
        var targetPlaceholders = ExtractPlaceholders(target);

        result.SourcePlaceholders = sourcePlaceholders;
        result.TargetPlaceholders = targetPlaceholders;

        // Check for missing placeholders
        foreach (var placeholder in sourcePlaceholders)
        {
            if (!targetPlaceholders.ContainsKey(placeholder.Key))
            {
                result.Errors.Add($"Missing placeholder '{placeholder.Key}' in target text");
            }
            else if (sourcePlaceholders[placeholder.Key] != targetPlaceholders[placeholder.Key])
            {
                result.Errors.Add($"Placeholder '{placeholder.Key}' count mismatch: source has {sourcePlaceholders[placeholder.Key]}, target has {targetPlaceholders[placeholder.Key]}");
            }
        }

        // Check for extra placeholders
        foreach (var placeholder in targetPlaceholders)
        {
            if (!sourcePlaceholders.ContainsKey(placeholder.Key))
            {
                result.Errors.Add($"Extra placeholder '{placeholder.Key}' in target text");
            }
        }

        // Check for unescaped quotes
        if (HasUnescapedQuotes(target))
        {
            result.Warnings.Add("Target text contains unescaped quotes");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <summary>
    /// Extract placeholders from text
    /// </summary>
    private static Dictionary<string, int> ExtractPlaceholders(string text)
    {
        var placeholders = new Dictionary<string, int>();
        var matches = PlaceholderRegex.Matches(text);

        foreach (Match match in matches)
        {
            var placeholder = match.Value;
            if (placeholders.ContainsKey(placeholder))
            {
                placeholders[placeholder]++;
            }
            else
            {
                placeholders[placeholder] = 1;
            }
        }

        return placeholders;
    }

    /// <summary>
    /// Check if text has unescaped quotes
    /// </summary>
    private static bool HasUnescapedQuotes(string text)
    {
        return UnescapedQuotesRegex.IsMatch(text);
    }

    /// <summary>
    /// Mask placeholders in text for AI translation
    /// </summary>
    public static (string maskedText, Dictionary<string, string> mapping) MaskPlaceholders(string text)
    {
        var mapping = new Dictionary<string, string>();
        var maskedText = text;
        var placeholderIndex = 0;

        var matches = PlaceholderRegex.Matches(text).Cast<Match>().OrderByDescending(m => m.Index);

        foreach (var match in matches)
        {
            var placeholder = match.Value;
            var mask = $"__PH_{placeholderIndex}__";
            mapping[mask] = placeholder;
            maskedText = maskedText.Substring(0, match.Index) + mask + maskedText.Substring(match.Index + match.Length);
            placeholderIndex++;
        }

        return (maskedText, mapping);
    }

    /// <summary>
    /// Unmask placeholders in translated text
    /// </summary>
    public static string UnmaskPlaceholders(string text, Dictionary<string, string> mapping)
    {
        var unmaskedText = text;
        foreach (var kvp in mapping)
        {
            unmaskedText = unmaskedText.Replace(kvp.Key, kvp.Value);
        }
        return unmaskedText;
    }
}
