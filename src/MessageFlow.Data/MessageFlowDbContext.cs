using MessageFlow.Core.Bible;
using MessageFlow.Core.ContentSources;
using MessageFlow.Core.Sermons;
using MessageFlow.Core.Songs;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Data;

public sealed class MessageFlowDbContext(DbContextOptions<MessageFlowDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();

    public DbSet<ContentSource> ContentSources => Set<ContentSource>();

    public DbSet<Sermon> Sermons => Set<Sermon>();

    public DbSet<SermonParagraph> SermonParagraphs => Set<SermonParagraph>();

    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();

    public DbSet<FavoriteParagraph> FavoriteParagraphs => Set<FavoriteParagraph>();

    public DbSet<ProjectionHistory> ProjectionHistories => Set<ProjectionHistory>();

    public DbSet<BibleTranslation> BibleTranslations => Set<BibleTranslation>();

    public DbSet<BibleBook> BibleBooks => Set<BibleBook>();

    public DbSet<BibleVerse> BibleVerses => Set<BibleVerse>();

    public DbSet<BibleFavoriteVerse> BibleFavoriteVerses => Set<BibleFavoriteVerse>();

    public DbSet<Song> Songs => Set<Song>();

    public DbSet<SongSection> SongSections => Set<SongSection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.ToTable("Authors");
            entity.HasKey(author => author.Id);

            entity.Property(author => author.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(author => author.DisplayName)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(author => author.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.HasIndex(author => author.FullName)
                .IsUnique();

            entity.HasData(new Author
            {
                Id = 1,
                FullName = "William Marrion Branham",
                DisplayName = "Brother Branham",
                Description = "Primary sermon author for the local MessageFlow sermon library."
            });
        });

        modelBuilder.Entity<ContentSource>(entity =>
        {
            entity.ToTable("ContentSources");
            entity.HasKey(source => source.Id);

            entity.Property(source => source.Name)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(source => source.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(source => source.SourceType)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(source => source.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(source => source.LocalFolderPath)
                .HasMaxLength(1024);

            entity.Property(source => source.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(source => source.Name)
                .IsUnique();

            entity.HasData(new ContentSource
            {
                Id = 1,
                Name = "brother_branham",
                DisplayName = "Brother Branham",
                SourceType = "SermonPdfCollection",
                Description = "Local Brother William Marrion Branham sermon PDF library.",
                LocalFolderPath = @"D:\Br William Marrion Branham\PDF",
                CreatedAt = new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        modelBuilder.Entity<Sermon>(entity =>
        {
            entity.ToTable("Sermons");
            entity.HasKey(sermon => sermon.Id);

            entity.Property(sermon => sermon.Title)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(sermon => sermon.SermonCode)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(sermon => sermon.Location)
                .HasMaxLength(200);

            entity.Property(sermon => sermon.Language)
                .HasMaxLength(20)
                .HasDefaultValue("en")
                .IsRequired();

            entity.Property(sermon => sermon.SourceFilePath)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(sermon => sermon.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(sermon => sermon.SourceFilePath)
                .IsUnique();

            entity.HasIndex(sermon => sermon.ContentSourceId);
            entity.HasIndex(sermon => sermon.Title);
            entity.HasIndex(sermon => sermon.SermonCode);
            entity.HasIndex(sermon => new { sermon.SermonCode, sermon.Year });
            entity.HasIndex(sermon => sermon.Year);
            entity.HasIndex(sermon => new { sermon.Year, sermon.Title });

            entity.HasOne(sermon => sermon.Author)
                .WithMany(author => author.Sermons)
                .HasForeignKey(sermon => sermon.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sermon => sermon.ContentSource)
                .WithMany(source => source.Sermons)
                .HasForeignKey(sermon => sermon.ContentSourceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SermonParagraph>(entity =>
        {
            entity.ToTable("SermonParagraphs");
            entity.HasKey(paragraph => paragraph.Id);

            entity.Property(paragraph => paragraph.Text)
                .IsRequired();

            entity.Property(paragraph => paragraph.SearchText)
                .IsRequired();

            entity.Property(paragraph => paragraph.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(paragraph => new { paragraph.SermonId, paragraph.ParagraphNumber })
                .IsUnique();

            entity.HasIndex(paragraph => paragraph.SermonId);
            entity.HasIndex(paragraph => paragraph.ParagraphNumber);
            entity.HasIndex(paragraph => paragraph.SearchText);
            entity.HasIndex(paragraph => paragraph.PageNumber);

            entity.HasOne(paragraph => paragraph.Sermon)
                .WithMany(sermon => sermon.Paragraphs)
                .HasForeignKey(paragraph => paragraph.SermonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ImportLog>(entity =>
        {
            entity.ToTable("ImportLogs");
            entity.HasKey(log => log.Id);

            entity.Property(log => log.FilePath)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(log => log.Status)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(log => log.Message)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(log => log.ImportedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(log => log.FilePath);
            entity.HasIndex(log => log.Status);
        });

        modelBuilder.Entity<FavoriteParagraph>(entity =>
        {
            entity.ToTable("FavoriteParagraphs");
            entity.HasKey(favorite => favorite.Id);

            entity.Property(favorite => favorite.CreatedAt)
                .IsRequired();

            entity.Property(favorite => favorite.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(favorite => favorite.SermonParagraphId)
                .IsUnique();

            entity.HasOne(favorite => favorite.SermonParagraph)
                .WithMany(paragraph => paragraph.Favorites)
                .HasForeignKey(favorite => favorite.SermonParagraphId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectionHistory>(entity =>
        {
            entity.ToTable("ProjectionHistories");
            entity.HasKey(history => history.Id);

            entity.Property(history => history.ProjectedAt)
                .IsRequired();

            entity.Property(history => history.SearchQuery)
                .HasMaxLength(500);

            entity.HasIndex(history => history.SermonParagraphId);
            entity.HasIndex(history => history.ProjectedAt);

            entity.HasOne(history => history.SermonParagraph)
                .WithMany(paragraph => paragraph.ProjectionHistories)
                .HasForeignKey(history => history.SermonParagraphId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BibleTranslation>(entity =>
        {
            entity.ToTable("BibleTranslations");
            entity.HasKey(translation => translation.Id);

            entity.Property(translation => translation.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(translation => translation.Abbreviation)
                .HasMaxLength(40)
                .IsRequired();

            entity.Property(translation => translation.Language)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(translation => translation.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(translation => translation.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(translation => translation.Abbreviation)
                .IsUnique();
        });

        modelBuilder.Entity<BibleBook>(entity =>
        {
            entity.ToTable("BibleBooks");
            entity.HasKey(book => book.Id);

            entity.Property(book => book.Name)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(book => book.ShortName)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(book => book.Name)
                .IsUnique();

            entity.HasIndex(book => book.ShortName);
            entity.HasIndex(book => book.BookOrder)
                .IsUnique();

            entity.HasData(BibleBookSeed.All);
        });

        modelBuilder.Entity<BibleVerse>(entity =>
        {
            entity.ToTable("BibleVerses");
            entity.HasKey(verse => verse.Id);

            entity.Property(verse => verse.Text)
                .IsRequired();

            entity.Property(verse => verse.SearchText)
                .IsRequired();

            entity.Property(verse => verse.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            entity.HasIndex(verse => new
                {
                    verse.TranslationId,
                    verse.BookId,
                    verse.Chapter,
                    verse.Verse
                })
                .IsUnique();

            entity.HasIndex(verse => verse.SearchText);

            entity.HasOne(verse => verse.BibleTranslation)
                .WithMany(translation => translation.Verses)
                .HasForeignKey(verse => verse.TranslationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(verse => verse.BibleBook)
                .WithMany(book => book.Verses)
                .HasForeignKey(verse => verse.BookId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BibleFavoriteVerse>(entity =>
        {
            entity.ToTable("BibleFavoriteVerses");
            entity.HasKey(favorite => favorite.Id);

            entity.Property(favorite => favorite.CreatedAt)
                .IsRequired();

            entity.Property(favorite => favorite.Notes)
                .HasMaxLength(1000);

            entity.HasIndex(favorite => favorite.BibleVerseId)
                .IsUnique();

            entity.HasIndex(favorite => favorite.CreatedAt);

            entity.HasOne(favorite => favorite.BibleVerse)
                .WithMany(verse => verse.Favorites)
                .HasForeignKey(favorite => favorite.BibleVerseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Song>(entity =>
        {
            entity.ToTable("Songs");
            entity.HasKey(song => song.Id);

            entity.Property(song => song.Title)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(song => song.NormalizedTitle)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(song => song.SourceFilePath)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(song => song.SourceFolder)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(song => song.FileName)
                .HasMaxLength(260)
                .IsRequired();

            entity.Property(song => song.ImportedAtUtc)
                .IsRequired();

            entity.Property(song => song.ContentHash)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(song => song.WarningSummary)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(song => song.Language)
                .HasMaxLength(20)
                .HasDefaultValue("en")
                .IsRequired();

            entity.Property(song => song.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            entity.HasIndex(song => song.SourceFilePath)
                .IsUnique();

            entity.HasIndex(song => song.NormalizedTitle);
            entity.HasIndex(song => song.ContentHash);
            entity.HasIndex(song => song.IsActive);
            entity.HasIndex(song => song.Language);
        });

        modelBuilder.Entity<SongSection>(entity =>
        {
            entity.ToTable("SongSections");
            entity.HasKey(section => section.Id);

            entity.Property(section => section.SectionType)
                .HasMaxLength(60)
                .IsRequired();

            entity.Property(section => section.SectionLabel)
                .HasMaxLength(120)
                .IsRequired();

            entity.Property(section => section.Text)
                .IsRequired();

            entity.Property(section => section.NormalizedText)
                .IsRequired();

            entity.HasIndex(section => new { section.SongId, section.SectionOrder })
                .IsUnique();

            entity.HasIndex(section => section.SectionType);
            entity.HasIndex(section => section.NormalizedText);

            entity.HasOne(section => section.Song)
                .WithMany(song => song.Sections)
                .HasForeignKey(section => section.SongId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
