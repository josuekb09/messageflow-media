using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Data.Migrations;

/// <summary>
/// Gives every document its own content type.
/// <para>
/// Until now the type lived on <c>ContentSources.SourceType</c>, so one source could hold only
/// one kind of document. The Brother Frank library holds circular letters, meeting transcripts
/// and books side by side, so the type has to belong to the document instead.
/// </para>
/// <para>
/// The column is nullable and additive: existing rows keep working, and callers fall back to the
/// owning source's <c>SourceType</c> while a row is still unclassified. The backfill is mirrored
/// in <c>MessageFlowDatabaseRepair</c>, which is what actually runs on installed machines.
/// </para>
/// </summary>
[Migration("20260921100000_AddSermonContentType")]
public partial class AddSermonContentType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Sermons"
            ADD COLUMN "ContentType" TEXT NULL;
            """);

        // Classify existing rows from metadata the database already holds: the owning source's
        // declared type, and the publication code assigned at import. Nothing is invented.
        migrationBuilder.Sql(
            """
            UPDATE "Sermons"
            SET "ContentType" = CASE
                WHEN (
                    SELECT "SourceType"
                    FROM "ContentSources"
                    WHERE "ContentSources"."Id" = "Sermons"."ContentSourceId"
                ) = 'SermonPdfCollection' THEN 'Sermon'
                WHEN "SermonCode" LIKE 'CL-%' THEN 'CircularLetter'
                -- GLOB, not LIKE: '_' is a single-character wildcard in LIKE, so a pattern of
                -- underscores also matched titled codes such as EF-TIME-IS-AT-HAND.
                WHEN "SermonCode" GLOB 'EF-[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]-*' THEN 'Meeting'
                WHEN "SermonCode" LIKE 'EF-%' THEN 'Book'
                ELSE (
                    SELECT "SourceType"
                    FROM "ContentSources"
                    WHERE "ContentSources"."Id" = "Sermons"."ContentSourceId"
                )
            END
            WHERE "ContentType" IS NULL;
            """);

        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS "IX_Sermons_ContentType"
            ON "Sermons" ("ContentType");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Sermons_ContentType";""");
        migrationBuilder.DropColumn(name: "ContentType", table: "Sermons");
    }
}
