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
    public static Task<ObservableCollection<T>> ToObservableCollectionAsync<T>(this IEnumerable<T> source)
    {
        // Use constructor for single allocation + single CollectionChanged event
        // Much faster than Task.Run + N Add() calls with N events
        return Task.FromResult(new ObservableCollection<T>(source));
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
