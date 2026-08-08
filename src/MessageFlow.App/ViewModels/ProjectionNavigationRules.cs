namespace MessageFlow.App.ViewModels;

public static class ProjectionNavigationRules
{
    public static bool ShouldUpdateLiveBibleVerse(
        bool isProjectionOpen,
        ProjectedContentSnapshot? liveSnapshot,
        int translationId,
        string translationAbbreviation,
        int verseId)
    {
        return isProjectionOpen &&
               liveSnapshot is { ContentType: ProjectionContentType.Bible } &&
               liveSnapshot.SourceId == translationId &&
               string.Equals(liveSnapshot.SourceKey, translationAbbreviation, StringComparison.OrdinalIgnoreCase) &&
               liveSnapshot.ItemId == verseId;
    }

    public static bool ShouldUpdateLiveSongSection(
        bool isProjectionOpen,
        ProjectedContentSnapshot? liveSnapshot,
        int songId,
        string songTitle,
        int sectionId)
    {
        return isProjectionOpen &&
               liveSnapshot is { ContentType: ProjectionContentType.Song } &&
               liveSnapshot.SourceId == songId &&
               string.Equals(liveSnapshot.SourceKey, songTitle, StringComparison.Ordinal) &&
               liveSnapshot.ItemId == sectionId;
    }

    public static bool ShouldUpdateLiveSermonParagraph(
        bool isProjectionOpen,
        ProjectedContentSnapshot? liveSnapshot,
        int selectedSermonId,
        int currentParagraphId)
    {
        return isProjectionOpen &&
               liveSnapshot is { ContentType: ProjectionContentType.Sermon } &&
               liveSnapshot.SourceId == selectedSermonId &&
               liveSnapshot.ItemId == currentParagraphId;
    }
}
