namespace MessageFlow.App.ViewModels;

public sealed class TestDataCleanupPreview
{
    public TestDataCleanupPreview(IReadOnlyList<TestDataCleanupSourcePreview> sources)
    {
        Sources = sources;
    }

    public IReadOnlyList<TestDataCleanupSourcePreview> Sources { get; }

    public int SourceCount => Sources.Count;

    public int DocumentCount => Sources.Sum(source => source.DocumentCount);

    public int ParagraphCount => Sources.Sum(source => source.ParagraphCount);

    public int FavoriteCount => Sources.Sum(source => source.FavoriteCount);

    public int HistoryCount => Sources.Sum(source => source.HistoryCount);

    public string Summary =>
        SourceCount == 0
            ? "No test sources were found."
            : $"{SourceCount:N0} test source(s), {DocumentCount:N0} document(s), {ParagraphCount:N0} paragraph(s), {FavoriteCount:N0} favorite(s), {HistoryCount:N0} history item(s).";
}

public sealed record TestDataCleanupSourcePreview(
    int SourceId,
    string DisplayName,
    string SourceTypeDisplay,
    string Folder,
    int DocumentCount,
    int ParagraphCount,
    int FavoriteCount,
    int HistoryCount);
