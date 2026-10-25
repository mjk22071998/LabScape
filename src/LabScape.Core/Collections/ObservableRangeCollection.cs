using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LabScape.Core.Collections;

/// <summary>
/// An ObservableCollection that supports batch updates and range operations
/// with a single Reset notification to prevent O(N) UI layout recalculations.
/// </summary>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification = false;

    public ObservableRangeCollection() : base() { }

    public ObservableRangeCollection(IEnumerable<T> collection) : base(collection) { }

    public ObservableRangeCollection(List<T> list) : base(list) { }

    /// <summary>
    /// Executes a batch modification action, suppressing individual notifications until complete,
    /// and then raises a single Reset notification.
    /// </summary>
    public void BatchUpdate(Action updateAction)
    {
        if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));
        CheckReentrancy();

        _suppressNotification = true;
        try
        {
            updateAction();
        }
        finally
        {
            _suppressNotification = false;
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Adds a range of items and raises a single Reset notification.
    /// </summary>
    public void AddRange(IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        CheckReentrancy();

        var list = collection.ToList();
        if (list.Count == 0) return;

        _suppressNotification = true;
        try
        {
            foreach (var item in list)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Replaces the current contents with the specified collection and raises a single Reset notification.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        CheckReentrancy();

        var list = collection.ToList();

        _suppressNotification = true;
        try
        {
            Items.Clear();
            foreach (var item in list)
            {
                Items.Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }
}
