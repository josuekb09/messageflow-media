using System.Collections.Specialized;
using MessageFlow.App.ViewModels;

namespace MessageFlow.Tests;

/// <summary>
/// Bug C (docs/BUGS_UX_2026-11-07.md): typing in a search box must not empty and refill the list
/// item by item on the UI thread, and every search box uses the same ~200 ms debounce.
/// </summary>
public class SearchResponsivenessTests
{
    [Fact]
    public void ReplaceAll_swaps_contents_with_a_single_notification()
    {
        var items = new BulkObservableCollection<int> { 1, 2, 3 };
        var events = new List<NotifyCollectionChangedAction>();
        items.CollectionChanged += (_, e) => events.Add(e.Action);

        items.ReplaceAll(Enumerable.Range(10, 200));

        Assert.Single(events);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[0]);
        Assert.Equal(Enumerable.Range(10, 200), items);
    }

    [Fact]
    public void Bible_lists_are_bulk_collections_so_a_new_result_replaces_the_old_in_one_pass()
    {
        foreach (var name in new[] { "BibleNavigationItems", "BibleResults" })
        {
            var property = typeof(MainViewModel).GetProperty(name);
            Assert.NotNull(property);
            Assert.True(
                property!.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(BulkObservableCollection<>),
                $"{name} must be a BulkObservableCollection, not Clear()+Add() per item.");
        }
    }

    [Fact]
    public void All_search_boxes_share_one_200ms_debounce()
    {
        Assert.Equal(200, MainViewModel.SearchDebounceMilliseconds);
        Assert.Equal(200, MainViewModel.BibleSearchDebounceMilliseconds);
        Assert.Equal(200, MainViewModel.SermonWithinSearchDebounceMilliseconds);
    }
}
