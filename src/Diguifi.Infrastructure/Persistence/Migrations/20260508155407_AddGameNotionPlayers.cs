using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diguifi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameNotionPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameNotionPlayers",
                columns: table => new
                {
                    PlayerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastPing = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameNotionPlayers", x => x.PlayerId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameNotionPlayers");
        }
    }
}
