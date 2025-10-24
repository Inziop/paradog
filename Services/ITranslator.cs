using System.Threading;
using System.Threading.Tasks;
using ParadoxTranslator.Models;

namespace ParadoxTranslator.Services;

/// <summary>
/// Abstraction for translation backends (AI or local)
/// </summary>
public interface ITranslator
{
    Task<string> TranslateAsync(string text, string sourceLang, string targetLang, TranslationConfig config, CancellationToken cancellationToken = default);
}
