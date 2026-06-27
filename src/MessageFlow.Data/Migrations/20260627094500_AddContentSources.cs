using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Data.Migrations
{
    [DbContext(typeof(MessageFlowDbContext))]
    [Migration("20260627094500_AddContentSources")]
    public partial class AddContentSources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "ContentSources" (
                    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    "Name" TEXT NOT NULL,
                    "DisplayName" TEXT NOT NULL,
                    "SourceType" TEXT NOT NULL,
                    "Description" TEXT NOT NULL,
                    "LocalFolderPath" TEXT NULL,
                    "CreatedAt" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_ContentSources_Name"
                ON "ContentSources" ("Name");
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "ContentSources" (
                    "Id",
                    "Name",
                    "DisplayName",
                    "SourceType",
                    "Description",
                    "LocalFolderPath",
                    "CreatedAt"
                )
                VALUES (
                    1,
                    'brother_branham',
                    'Brother Branham',
                    'SermonPdfCollection',
                    'Local Brother William Marrion Branham sermon PDF library.',
                    'D:\Br William Marrion Branham\PDF',
                    CURRENT_TIMESTAMP
                )
                ON CONFLICT("Name") DO UPDATE SET
                    "DisplayName" = excluded."DisplayName",
                    "SourceType" = excluded."SourceType",
                    "Description" = excluded."Description",
                    "LocalFolderPath" = excluded."LocalFolderPath";
                """);

            migrationBuilder.AddColumn<int>(
                name: "ContentSourceId",
                table: "Sermons",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Sermons"
                SET "ContentSourceId" = (
                    SELECT "Id"
                    FROM "ContentSources"
                    WHERE "Name" = 'brother_branham'
                    LIMIT 1
                )
                WHERE "ContentSourceId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_Sermons_ContentSourceId"
                ON "Sermons" ("ContentSourceId");
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sermons_ContentSourceId",
                table: "Sermons");

            migrationBuilder.DropColumn(
                name: "ContentSourceId",
                table: "Sermons");

            migrationBuilder.DropTable(
                name: "ContentSources");
        }
    }
}
