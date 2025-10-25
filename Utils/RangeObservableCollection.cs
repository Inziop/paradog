using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ParadoxTranslator.Utils;

/// <summary>
/// ObservableCollection with bulk operation support to reduce UI update overhead
/// </summary>
public class RangeObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification = false;

    /// <summary>
    /// Add multiple items at once with single notification
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;

        foreach (var item in items)
        {
            Add(item);
        }

        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Remove multiple items at once with single notification
    /// </summary>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;

        foreach (var item in items)
        {
            Remove(item);
        }

        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Clear and add new items with single notification
    /// </summary>
    public void Reset(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        Clear();

        foreach (var item in items)
        {
            Add(item);
        }

        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnPropertyChanged(e);
        }
    }
}
