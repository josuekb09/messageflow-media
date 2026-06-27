using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MessageFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Authors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Authors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sermons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AuthorId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SermonCode = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Language = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "en"),
                    SourceFilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sermons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sermons_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SermonParagraphs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SermonId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParagraphNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SearchText = table.Column<string>(type: "TEXT", nullable: false),
                    PageNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SermonParagraphs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SermonParagraphs_Sermons_SermonId",
                        column: x => x.SermonId,
                        principalTable: "Sermons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Authors",
                columns: new[] { "Id", "Description", "DisplayName", "FullName" },
                values: new object[] { 1, "Primary sermon author for the local MessageFlow sermon library.", "Brother Branham", "William Marrion Branham" });

            migrationBuilder.CreateIndex(
                name: "IX_Authors_FullName",
                table: "Authors",
                column: "FullName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_FilePath",
                table: "ImportLogs",
                column: "FilePath");

            migrationBuilder.CreateIndex(
                name: "IX_ImportLogs_Status",
                table: "ImportLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SermonParagraphs_SermonId_ParagraphNumber",
                table: "SermonParagraphs",
                columns: new[] { "SermonId", "ParagraphNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_AuthorId",
                table: "Sermons",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_SermonCode",
                table: "Sermons",
                column: "SermonCode");

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_SourceFilePath",
                table: "Sermons",
                column: "SourceFilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sermons_Year",
                table: "Sermons",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportLogs");

            migrationBuilder.DropTable(
                name: "SermonParagraphs");

            migrationBuilder.DropTable(
                name: "Sermons");

            migrationBuilder.DropTable(
                name: "Authors");
        }
    }
}
