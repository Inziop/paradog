using System.Collections.ObjectModel;
using System.IO;

namespace ParadoxTranslator.Utils;

/// <summary>
/// Helper methods for async operations
/// </summary>
public static class AsyncHelpers
{
    /// <summary>
    /// Convert IEnumerable to ObservableCollection asynchronously
    /// </summary>
    public static async Task<ObservableCollection<T>> ToObservableCollectionAsync<T>(this IEnumerable<T> source)
    {
        var collection = new ObservableCollection<T>();
        await Task.Run(() =>
        {
            foreach (var item in source)
            {
                collection.Add(item);
            }
        });
        return collection;
    }

    /// <summary>
    /// Safe async file operations
    /// </summary>
    public static async Task<bool> TryWriteAllTextAsync(string path, string contents)
    {
        try
        {
            await File.WriteAllTextAsync(path, contents);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safe async file reading
    /// </summary>
    public static async Task<string?> TryReadAllTextAsync(string path)
    {
        try
        {
            return await File.ReadAllTextAsync(path);
        }
        catch
        {
            return null;
        }
    }
}
