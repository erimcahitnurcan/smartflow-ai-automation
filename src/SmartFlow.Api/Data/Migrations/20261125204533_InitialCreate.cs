using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SupportRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalText = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    SenderEmail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    SuggestedAction = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_Category",
                table: "SupportRequests",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_CreatedAtUtc",
                table: "SupportRequests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupportRequests_Priority",
                table: "SupportRequests",
                column: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportRequests");
        }
    }
}
