using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

/// <summary>
/// Service for translating text using various AI engines
/// </summary>
public class TranslationService
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore;
    private readonly TranslationConfig _config;

    public TranslationService(TranslationConfig config)
    {
        _config = config;
        _httpClient = new HttpClient();
        _semaphore = new SemaphoreSlim(_config.MaxConcurrentRequests, _config.MaxConcurrentRequests);
    }

    /// <summary>
    /// Translate a single text
    /// </summary>
    public async Task<TranslationResult> TranslateTextAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var startTime = DateTime.UtcNow;

            // Mask placeholders
            var (maskedText, mapping) = PlaceholderValidator.MaskPlaceholders(text);

            string translatedText;
            string engine;

            switch (_config.SelectedEngine.ToLower())
            {
                case "google":
                    translatedText = await TranslateWithGoogleAsync(maskedText, sourceLang, targetLang, cancellationToken);
                    engine = "Google Translate";
                    break;
                case "gemini":
                    translatedText = await TranslateWithGeminiAsync(maskedText, sourceLang, targetLang, cancellationToken);
                    engine = "Gemini";
                    break;
                case "deepl":
                    translatedText = await TranslateWithDeepLAsync(maskedText, sourceLang, targetLang, cancellationToken);
                    engine = "DeepL";
                    break;
                default:
                    throw new NotSupportedException($"Translation engine '{_config.SelectedEngine}' is not supported");
            }

            // Unmask placeholders
            translatedText = PlaceholderValidator.UnmaskPlaceholders(translatedText, mapping);

            return new TranslationResult
            {
                Success = true,
                TranslatedText = translatedText,
                SourceLanguage = sourceLang,
                TargetLanguage = targetLang,
                Duration = DateTime.UtcNow - startTime,
                Engine = engine
            };
        }
        catch (Exception ex)
        {
            return new TranslationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                SourceLanguage = sourceLang,
                TargetLanguage = targetLang
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Translate multiple entries in batch
    /// </summary>
    public async Task<List<TranslationResult>> TranslateBatchAsync(
        IEnumerable<LocalizationEntry> entries, 
        string sourceLang, 
        string targetLang,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TranslationResult>();
        var entryList = entries.ToList();
        var total = entryList.Count;
        var completed = 0;

        foreach (var entry in entryList)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Skip if already translated and not overwriting
            if (!string.IsNullOrWhiteSpace(entry.Target) && !_config.OverwriteExistingTranslations)
            {
                results.Add(new TranslationResult { Success = true, TranslatedText = entry.Target });
                completed++;
                progress?.Report(completed * 100 / total);
                continue;
            }

            var result = await TranslateTextAsync(entry.Source, sourceLang, targetLang, cancellationToken);
            results.Add(result);

            if (result.Success)
            {
                entry.Target = result.TranslatedText;
            }

            completed++;
            progress?.Report(completed * 100 / total);
        }

        return results;
    }

    private async Task<string> TranslateWithGoogleAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken)
    {
        // Google Translate API implementation
        var requestBody = new
        {
            q = text,
            source = sourceLang,
            target = targetLang,
            format = "text"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.GoogleApiKey);

        var response = await _httpClient.PostAsync("https://translation.googleapis.com/language/translate/v2", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        return responseObj.GetProperty("data").GetProperty("translations")[0].GetProperty("translatedText").GetString() ?? text;
    }

    private async Task<string> TranslateWithGeminiAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken)
    {
        var prompt = $"Translate the following text from {sourceLang} to {targetLang}. Preserve all placeholders like {{0}}, {{1}}, %s, %d, $VARIABLE$, [Root.GetName], and HTML tags exactly as they are. Only translate the actual text content:\n\n{text}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 1000
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config.GeminiApiKey);

        var response = await _httpClient.PostAsync(_config.GeminiEndpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        return responseObj.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? text;
    }

    private async Task<string> TranslateWithDeepLAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken)
    {
        var formData = new List<KeyValuePair<string, string>>
        {
            new("text", text),
            new("source_lang", sourceLang.ToUpper()),
            new("target_lang", targetLang.ToUpper())
        };

        var content = new FormUrlEncodedContent(formData);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", _config.DeepLApiKey);

        var response = await _httpClient.PostAsync("https://api-free.deepl.com/v2/translate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        return responseObj.GetProperty("translations")[0].GetProperty("text").GetString() ?? text;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _semaphore?.Dispose();
    }
}
