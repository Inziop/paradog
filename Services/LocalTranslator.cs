using System.Threading;
using System.Threading.Tasks;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

/// <summary>
/// Local translator (no-op) used when AI is disabled. Returns input text unchanged.
/// </summary>
public class LocalTranslator : ITranslator
{
    public Task<string> TranslateAsync(string text, string sourceLang, string targetLang, TranslationConfig config, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(text);
    }
}
