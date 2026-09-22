using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MessageFlow.App.ViewModels;

/// <summary>
/// An <see cref="ObservableCollection{T}"/> that can swap its whole contents in one notification.
/// </summary>
/// <remarks>
/// Adding items one at a time to a collection bound to a Selector makes WPF run its selection
/// bookkeeping for every single add, which is why filling the sermon list with a whole library
/// (over 1200 documents) stalled the UI thread for seconds. Replacing the contents behind a single
/// Reset lets the list rebuild once.
/// </remarks>
public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    /// <summary>
    /// Replaces every item with <paramref name="items"/>, raising one Reset instead of one
    /// notification per item.
    /// </summary>
    public void ReplaceAll(IEnumerable<T> items)
    {
        CheckReentrancy();

        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }

        // Items is the raw backing list, so it raises nothing on its own. Count and the indexer
        // must be announced by hand, exactly as the base Clear/Add overrides would.
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
