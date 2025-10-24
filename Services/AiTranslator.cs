using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

/// <summary>
/// Translator implementation that calls external AI translation providers.
/// Supports Google Translate (v2), DeepL and Gemini/OpenAI-style endpoints.
/// If required API key is missing, falls back to a mock mode (returns original text).
/// </summary>
public class AiTranslator : ITranslator
{
    private readonly System.Net.Http.HttpClient _httpClient;

    public AiTranslator(System.Net.Http.HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new System.Net.Http.HttpClient();
    }

    public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang, TranslationConfig config, CancellationToken cancellationToken = default)
    {
        var engine = (config.SelectedEngine ?? string.Empty).ToLowerInvariant();

        // Support explicit Mock mode for preview/testing
        if (config.MockMode)
            return MockTranslate(text, sourceLang, targetLang);

        // Fall back to no-op if AI disabled or engine not set
        if (!config.EnableAi) return text;

        // small retry/backoff loop to improve reliability
        var attempts = 0;
        var maxAttempts = 3;
        var backoffMs = 500;

        while (true)
        {
            attempts++;
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (config.TimeoutSeconds > 0)
                    cts.CancelAfter(TimeSpan.FromSeconds(config.TimeoutSeconds));

                switch (engine)
                {
                    case "google":
                        if (string.IsNullOrWhiteSpace(config.GoogleApiKey))
                            return text;
                        return await TranslateWithGoogleAsync(text, sourceLang, targetLang, config.GoogleApiKey, cts.Token);
                    case "deepl":
                        if (string.IsNullOrWhiteSpace(config.DeepLApiKey))
                            return text;
                        return await TranslateWithDeepLAsync(text, sourceLang, targetLang, config.DeepLApiKey, cts.Token);
                    case "gemini":
                    case "openai":
                    case "openai-gemini":
                        if (string.IsNullOrWhiteSpace(config.GeminiApiKey))
                            return text;
                        return await TranslateWithGeminiAsync(text, sourceLang, targetLang, config.GeminiApiKey, config.GeminiEndpoint, cts.Token);
                    default:
                        return text;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // propagate user cancellation
            }
            catch (Exception)
            {
                if (attempts >= maxAttempts)
                    return text; // give up and return original

                // backoff then retry
                await Task.Delay(backoffMs, cancellationToken);
                backoffMs *= 2;
            }
        }
    }

    private async Task<string> TranslateWithGoogleAsync(string text, string sourceLang, string targetLang, string apiKey, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            q = text,
            source = sourceLang,
            target = targetLang,
            format = "text"
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsync("https://translation.googleapis.com/language/translate/v2", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        return responseObj.GetProperty("data").GetProperty("translations")[0].GetProperty("translatedText").GetString() ?? text;
    }

    private async Task<string> TranslateWithGeminiAsync(string text, string sourceLang, string targetLang, string apiKey, string endpoint, CancellationToken cancellationToken)
    {
        // Enhanced prompt for more natural and accurate translations
        var prompt = $@"You are an expert translator for games and software. Translate the following text from {sourceLang} to {targetLang}.

Rules:
1. Translate naturally while preserving the exact meaning
2. Keep ALL placeholders unchanged (examples: {{0}}, %d, [Root.GetName], $VAR$)
3. Maintain placeholder order and casing
4. ONLY output the translated text, nothing else
5. DO NOT copy the source text if unsure - respond with 'TRANSLATION_ERROR' instead

Text to translate:
{text}

Your translation:";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
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

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        // Support both Gemini and generic OpenAI-like shapes if possible
        if (responseObj.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var contentEl = candidates[0].GetProperty("content");
            if (contentEl.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
            {
                return parts[0].GetProperty("text").GetString() ?? text;
            }
        }

        // Fallback: try some common fields
        if (responseObj.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("text", out var t))
                return t.GetString() ?? text;
        }

        return text;
    }

    private string MockTranslate(string text, string sourceLang, string targetLang)
    {
        // Simple, deterministic mock transformation for preview: wrap lines and mark as MOCK
        if (string.IsNullOrEmpty(text)) return text;
        return $"[MOCK {targetLang}] " + text;
    }

    private async Task<string> TranslateWithDeepLAsync(string text, string sourceLang, string targetLang, string apiKey, CancellationToken cancellationToken)
    {
        var formData = new List<KeyValuePair<string, string>>
        {
            new("text", text),
            new("source_lang", sourceLang.ToUpper()),
            new("target_lang", targetLang.ToUpper())
        };

        var content = new FormUrlEncodedContent(formData);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("DeepL-Auth-Key", apiKey);

        var response = await _httpClient.PostAsync("https://api-free.deepl.com/v2/translate", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        return responseObj.GetProperty("translations")[0].GetProperty("text").GetString() ?? text;
    }

    public void Dispose()
    {
        try { _httpClient?.Dispose(); } catch { }
    }
}
