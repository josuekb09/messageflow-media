using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MessageFlowDbContext))]
    [Migration("20260627090000_AddFavoritesAndProjectionHistory")]
    public partial class AddFavoritesAndProjectionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "FavoriteParagraphs" (
                    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    "SermonParagraphId" INTEGER NOT NULL,
                    "CreatedAt" TEXT NOT NULL,
                    "Notes" TEXT NULL,
                    FOREIGN KEY ("SermonParagraphId") REFERENCES "SermonParagraphs" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "ProjectionHistories" (
                    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    "SermonParagraphId" INTEGER NOT NULL,
                    "ProjectedAt" TEXT NOT NULL,
                    "SearchQuery" TEXT NULL,
                    FOREIGN KEY ("SermonParagraphId") REFERENCES "SermonParagraphs" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_FavoriteParagraphs_SermonParagraphId"
                ON "FavoriteParagraphs" ("SermonParagraphId");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_ProjectionHistories_ProjectedAt"
                ON "ProjectionHistories" ("ProjectedAt");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_ProjectionHistories_SermonParagraphId"
                ON "ProjectionHistories" ("SermonParagraphId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteParagraphs");

            migrationBuilder.DropTable(
                name: "ProjectionHistories");
        }
    }
}
