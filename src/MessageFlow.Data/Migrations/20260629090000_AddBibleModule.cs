using System;
using MessageFlow.Core.Bible;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Data.Migrations
{
    [DbContext(typeof(MessageFlowDbContext))]
    [Migration("20260629090000_AddBibleModule")]
    public partial class AddBibleModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BibleTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Abbreviation = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleTranslations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BibleBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    BookOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BibleVerses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TranslationId = table.Column<int>(type: "INTEGER", nullable: false),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    Chapter = table.Column<int>(type: "INTEGER", nullable: false),
                    Verse = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SearchText = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BibleVerses_BibleBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BibleVerses_BibleTranslations_TranslationId",
                        column: x => x.TranslationId,
                        principalTable: "BibleTranslations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            foreach (var book in BibleBookSeed.All)
            {
                migrationBuilder.InsertData(
                    table: "BibleBooks",
                    columns: ["Id", "Name", "ShortName", "BookOrder"],
                    values: [book.Id, book.Name, book.ShortName, book.BookOrder]);
            }

            migrationBuilder.CreateIndex(
                name: "IX_BibleTranslations_Abbreviation",
                table: "BibleTranslations",
                column: "Abbreviation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibleBooks_Name",
                table: "BibleBooks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibleBooks_ShortName",
                table: "BibleBooks",
                column: "ShortName");

            migrationBuilder.CreateIndex(
                name: "IX_BibleBooks_BookOrder",
                table: "BibleBooks",
                column: "BookOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibleVerses_TranslationId_BookId_Chapter_Verse",
                table: "BibleVerses",
                columns: ["TranslationId", "BookId", "Chapter", "Verse"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibleVerses_BookId",
                table: "BibleVerses",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BibleVerses_SearchText",
                table: "BibleVerses",
                column: "SearchText");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BibleVerses");
            migrationBuilder.DropTable(name: "BibleTranslations");
            migrationBuilder.DropTable(name: "BibleBooks");
        }
    }
}
