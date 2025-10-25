using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

public class TranslationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore;
    private readonly TranslationConfig _config;
    private readonly ITranslator _translator;

    public TranslationService(TranslationConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = new HttpClient();
        _semaphore = new SemaphoreSlim(Math.Max(1, _config.MaxConcurrentRequests));
        _translator = _config.EnableAi ? (ITranslator)new AiTranslator(_httpClient) : new LocalTranslator();
    }

    public async Task<TranslationResult> TranslateTextAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var start = DateTime.UtcNow;

            var (maskedText, mapping) = PlaceholderValidator.MaskPlaceholders(text);

            var translated = await _translator.TranslateAsync(maskedText, sourceLang, targetLang, _config, cancellationToken).ConfigureAwait(false);

            translated = PlaceholderValidator.UnmaskPlaceholders(translated, mapping);

            return new TranslationResult
            {
                Success = true,
                TranslatedText = translated,
                SourceLanguage = sourceLang,
                TargetLanguage = targetLang,
                Duration = DateTime.UtcNow - start,
                Engine = _config.SelectedEngine
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

    public async Task<List<TranslationResult>> TranslateBatchAsync(IEnumerable<LocalizationEntry> entries, string sourceLang, string targetLang, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var list = entries.ToList();
        var total = list.Count;
        var results = new List<TranslationResult>(total);
        var completed = 0;

        foreach (var entry in list)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (!string.IsNullOrWhiteSpace(entry.TranslatedText) && !_config.OverwriteExistingTranslations)
            {
                results.Add(new TranslationResult { Success = true, TranslatedText = entry.TranslatedText });
                completed++;
                progress?.Report(completed * 100 / Math.Max(1, total));
                continue;
            }

            var res = await TranslateTextAsync(entry.SourceText, sourceLang, targetLang, cancellationToken).ConfigureAwait(false);
            if (res.Success) entry.TranslatedText = res.TranslatedText;
            results.Add(res);

            completed++;
            progress?.Report(completed * 100 / Math.Max(1, total));
        }

        return results;
    }

    public void Dispose()
    {
        try { (_translator as IDisposable)?.Dispose(); } catch { }
        try { _httpClient?.Dispose(); } catch { }
        try { _semaphore?.Dispose(); } catch { }
    }
}
