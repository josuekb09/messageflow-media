using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Data.Migrations;

[Migration("20260630120500_AddBibleFavoriteVerses")]
public partial class AddBibleFavoriteVerses : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "BibleFavoriteVerses" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_BibleFavoriteVerses" PRIMARY KEY AUTOINCREMENT,
                "BibleVerseId" INTEGER NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                "Notes" TEXT NULL,
                CONSTRAINT "FK_BibleFavoriteVerses_BibleVerses_BibleVerseId"
                    FOREIGN KEY ("BibleVerseId") REFERENCES "BibleVerses" ("Id") ON DELETE CASCADE
            );
            """);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_BibleFavoriteVerses_BibleVerseId"
            ON "BibleFavoriteVerses" ("BibleVerseId");
            """);

        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS "IX_BibleFavoriteVerses_CreatedAt"
            ON "BibleFavoriteVerses" ("CreatedAt");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BibleFavoriteVerses");
    }
}
