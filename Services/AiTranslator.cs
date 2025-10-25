using System.Diagnostics;
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



        // Fall back to no-op if AI disabled or engine not set
        if (!config.EnableAi) throw new InvalidOperationException("AI translation is disabled");

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
                            throw new InvalidOperationException("Google API key is not configured");
                        return await TranslateWithGoogleAsync(text, sourceLang, targetLang, config.GoogleApiKey, cts.Token);
                    case "deepl":
                        if (string.IsNullOrWhiteSpace(config.DeepLApiKey))
                            throw new InvalidOperationException("DeepL API key is not configured");
                        return await TranslateWithDeepLAsync(text, sourceLang, targetLang, config.DeepLApiKey, cts.Token);
                    case "gemini":
                    case "openai":
                    case "openai-gemini":
                        if (string.IsNullOrWhiteSpace(config.GeminiApiKey))
                            throw new InvalidOperationException("Gemini API key is not configured");
                        return await TranslateWithGeminiAsync(text, sourceLang, targetLang, config.GeminiApiKey, config.GeminiEndpoint, cts.Token);
                    default:
                        throw new InvalidOperationException($"Unsupported translation engine: {engine}");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // propagate user cancellation
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AiTranslator] Translation attempt {attempts} failed. Error: {ex}");

                if (attempts >= maxAttempts)
                {
                    Debug.WriteLine($"[AiTranslator] Max attempts reached. Giving up and returning original text.");
                    throw new Exception("AI translation failed after multiple attempts. Please check your configuration and network.", ex);
                }

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
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        Debug.WriteLine($"[AiTranslator] Google Raw Response: {responseJson}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Google Translate API request failed with status code {response.StatusCode}. Response: {responseJson}");
        }

        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        if (responseObj.TryGetProperty("data", out var data) &&
            data.TryGetProperty("translations", out var translations) &&
            translations.GetArrayLength() > 0 &&
            translations[0].TryGetProperty("translatedText", out var translatedText))
        {
            return translatedText.GetString() ?? string.Empty;
        }

        throw new JsonException("Failed to parse translated text from Google Translate API response.");
    }

    private async Task<string> TranslateWithGeminiAsync(string text, string sourceLang, string targetLang, string apiKey, string endpoint, CancellationToken cancellationToken)
    {
        // Enhanced prompt for more natural and accurate translations
        var rules = targetLang.ToLower() == "vi"
            ? $@"Bạn là chuyên gia dịch thuật game và phần mềm. Hãy dịch đoạn văn sau từ {sourceLang} sang {targetLang}.

Quy tắc:
1. Dịch tự nhiên nhưng phải giữ đúng ý nghĩa
2. Giữ nguyên TẤT CẢ các placeholder (ví dụ: {{{{0}}}}, %d, [Root.GetName], $VAR$)
3. Giữ đúng thứ tự và kiểu chữ (hoa/thường) của placeholder
4. CHỈ trả về bản dịch, không thêm nội dung khác
5. KHÔNG copy nguyên văn nếu không chắc chắn - trả về 'TRANSLATION_ERROR'"
            : $@"You are an expert translator for games and software. Translate the following text from {sourceLang} to {targetLang}.

Rules:
1. Translate naturally while preserving the exact meaning
2. Keep ALL placeholders unchanged (examples: {{{{0}}}}, %d, [Root.GetName], $VAR$)
3. Maintain placeholder order and casing
4. ONLY output the translated text, nothing else
5. DO NOT copy the source text if unsure - respond with 'TRANSLATION_ERROR' instead";

        var prompt = $@"{rules}

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

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        Debug.WriteLine($"[AiTranslator] Gemini Raw Response: {responseJson}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Gemini API request failed with status code {response.StatusCode}. Response: {responseJson}");
        }
        
        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        // Support both Gemini and generic OpenAI-like shapes if possible
        if (responseObj.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var contentEl = candidates[0].GetProperty("content");
            if (contentEl.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
            {
                var result = parts[0].GetProperty("text").GetString() ?? string.Empty;
                if (result.Trim().Equals("TRANSLATION_ERROR", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Gemini returned TRANSLATION_ERROR, indicating it could not translate the text.");
                }
                return result;
            }
        }

        // Fallback: try some common fields
        if (responseObj.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("text", out var t))
                return t.GetString() ?? string.Empty;
        }

        Debug.WriteLine($"[AiTranslator] Failed to parse Gemini response. Returning original text.");
        throw new JsonException("Failed to parse translated text from Gemini API response.");
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
        
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        Debug.WriteLine($"[AiTranslator] DeepL Raw Response: {responseJson}");

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"DeepL API request failed with status code {response.StatusCode}. Response: {responseJson}");
        }

        var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

        if (responseObj.TryGetProperty("translations", out var translations) &&
            translations.GetArrayLength() > 0 &&
            translations[0].TryGetProperty("text", out var translatedText))
        {
            return translatedText.GetString() ?? string.Empty;
        }
        
        throw new JsonException("Failed to parse translated text from DeepL API response.");
    }

    public void Dispose()
    {
        try { _httpClient?.Dispose(); } catch { }
    }
}
