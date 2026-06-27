using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSermonSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sermons_Title",
                table: "Sermons",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_Year_Title",
                table: "Sermons",
                columns: new[] { "Year", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_SermonParagraphs_PageNumber",
                table: "SermonParagraphs",
                column: "PageNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SermonParagraphs_ParagraphNumber",
                table: "SermonParagraphs",
                column: "ParagraphNumber");

            migrationBuilder.Sql(
                """
                CREATE VIRTUAL TABLE IF NOT EXISTS SermonParagraphSearch
                USING fts5(
                    Text,
                    SearchText,
                    content='SermonParagraphs',
                    content_rowid='Id',
                    tokenize='unicode61'
                );
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO SermonParagraphSearch(SermonParagraphSearch)
                VALUES('rebuild');
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS SermonParagraphs_ai
                AFTER INSERT ON SermonParagraphs
                BEGIN
                    INSERT INTO SermonParagraphSearch(rowid, Text, SearchText)
                    VALUES (new.Id, new.Text, new.SearchText);
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS SermonParagraphs_ad
                AFTER DELETE ON SermonParagraphs
                BEGIN
                    INSERT INTO SermonParagraphSearch(SermonParagraphSearch, rowid, Text, SearchText)
                    VALUES('delete', old.Id, old.Text, old.SearchText);
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER IF NOT EXISTS SermonParagraphs_au
                AFTER UPDATE ON SermonParagraphs
                BEGIN
                    INSERT INTO SermonParagraphSearch(SermonParagraphSearch, rowid, Text, SearchText)
                    VALUES('delete', old.Id, old.Text, old.SearchText);

                    INSERT INTO SermonParagraphSearch(rowid, Text, SearchText)
                    VALUES (new.Id, new.Text, new.SearchText);
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SermonParagraphs_au;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SermonParagraphs_ad;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS SermonParagraphs_ai;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS SermonParagraphSearch;");

            migrationBuilder.DropIndex(
                name: "IX_Sermons_Title",
                table: "Sermons");

            migrationBuilder.DropIndex(
                name: "IX_Sermons_Year_Title",
                table: "Sermons");

            migrationBuilder.DropIndex(
                name: "IX_SermonParagraphs_PageNumber",
                table: "SermonParagraphs");

            migrationBuilder.DropIndex(
                name: "IX_SermonParagraphs_ParagraphNumber",
                table: "SermonParagraphs");
        }
    }
}
