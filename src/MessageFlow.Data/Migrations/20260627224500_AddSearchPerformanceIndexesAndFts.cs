using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Data.Migrations
{
    [DbContext(typeof(MessageFlowDbContext))]
    [Migration("20260627224500_AddSearchPerformanceIndexesAndFts")]
    public partial class AddSearchPerformanceIndexesAndFts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Sermons_Title" ON "Sermons" ("Title");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Sermons_SermonCode" ON "Sermons" ("SermonCode");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Sermons_Year" ON "Sermons" ("Year");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Sermons_AuthorId" ON "Sermons" ("AuthorId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Sermons_ContentSourceId" ON "Sermons" ("ContentSourceId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_Sermons_SermonCode_Year" ON "Sermons" ("SermonCode", "Year");""");

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_SermonParagraphs_SermonId" ON "SermonParagraphs" ("SermonId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_SermonParagraphs_ParagraphNumber" ON "SermonParagraphs" ("ParagraphNumber");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_SermonParagraphs_SearchText" ON "SermonParagraphs" ("SearchText");""");
            migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_SermonParagraphs_SermonId_ParagraphNumber" ON "SermonParagraphs" ("SermonId", "ParagraphNumber");""");

            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE IF NOT EXISTS "SermonParagraphsFts"
                USING fts5(
                    ParagraphId UNINDEXED,
                    SermonId UNINDEXED,
                    Title,
                    SermonCode,
                    ParagraphNumber UNINDEXED,
                    SearchText,
                    tokenize='unicode61'
                );
                """);

            migrationBuilder.Sql("""DELETE FROM "SermonParagraphsFts";""");
            migrationBuilder.Sql(
                """
                INSERT INTO "SermonParagraphsFts" (
                    rowid,
                    ParagraphId,
                    SermonId,
                    Title,
                    SermonCode,
                    ParagraphNumber,
                    SearchText
                )
                SELECT
                    p."Id",
                    p."Id",
                    p."SermonId",
                    s."Title",
                    s."SermonCode",
                    p."ParagraphNumber",
                    p."SearchText"
                FROM "SermonParagraphs" p
                JOIN "Sermons" s ON s."Id" = p."SermonId";
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS "SermonParagraphsFts_ai"
                AFTER INSERT ON "SermonParagraphs"
                BEGIN
                    INSERT INTO "SermonParagraphsFts" (
                        rowid,
                        ParagraphId,
                        SermonId,
                        Title,
                        SermonCode,
                        ParagraphNumber,
                        SearchText
                    )
                    SELECT
                        new."Id",
                        new."Id",
                        new."SermonId",
                        s."Title",
                        s."SermonCode",
                        new."ParagraphNumber",
                        new."SearchText"
                    FROM "Sermons" s
                    WHERE s."Id" = new."SermonId";
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS "SermonParagraphsFts_ad"
                AFTER DELETE ON "SermonParagraphs"
                BEGIN
                    DELETE FROM "SermonParagraphsFts"
                    WHERE rowid = old."Id";
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS "SermonParagraphsFts_au"
                AFTER UPDATE ON "SermonParagraphs"
                BEGIN
                    DELETE FROM "SermonParagraphsFts"
                    WHERE rowid = old."Id";

                    INSERT INTO "SermonParagraphsFts" (
                        rowid,
                        ParagraphId,
                        SermonId,
                        Title,
                        SermonCode,
                        ParagraphNumber,
                        SearchText
                    )
                    SELECT
                        new."Id",
                        new."Id",
                        new."SermonId",
                        s."Title",
                        s."SermonCode",
                        new."ParagraphNumber",
                        new."SearchText"
                    FROM "Sermons" s
                    WHERE s."Id" = new."SermonId";
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP TRIGGER IF EXISTS "SermonParagraphsFts_au";""");
            migrationBuilder.Sql("""DROP TRIGGER IF EXISTS "SermonParagraphsFts_ad";""");
            migrationBuilder.Sql("""DROP TRIGGER IF EXISTS "SermonParagraphsFts_ai";""");
            migrationBuilder.Sql("""DROP TABLE IF EXISTS "SermonParagraphsFts";""");

            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SermonParagraphs_SearchText";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_SermonParagraphs_SermonId";""");
            migrationBuilder.Sql("""DROP INDEX IF EXISTS "IX_Sermons_SermonCode_Year";""");
        }
    }
}
