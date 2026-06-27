using MessageFlow.Core.Sermons;
using Microsoft.EntityFrameworkCore;

namespace MessageFlow.Data;

public sealed class MessageFlowDbContext(DbContextOptions<MessageFlowDbContext> options) : DbContext(options)
{
    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Sermon> Sermons => Set<Sermon>();

    public DbSet<SermonParagraph> SermonParagraphs => Set<SermonParagraph>();

    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();

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

            entity.HasIndex(sermon => sermon.Title);
            entity.HasIndex(sermon => sermon.SermonCode);
            entity.HasIndex(sermon => sermon.Year);
            entity.HasIndex(sermon => new { sermon.Year, sermon.Title });

            entity.HasOne(sermon => sermon.Author)
                .WithMany(author => author.Sermons)
                .HasForeignKey(sermon => sermon.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
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

            entity.HasIndex(paragraph => paragraph.ParagraphNumber);
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
    }
}
