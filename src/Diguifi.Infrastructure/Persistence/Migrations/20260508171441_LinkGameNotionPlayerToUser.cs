using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diguifi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkGameNotionPlayerToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "GameNotionPlayers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameNotionPlayers_UserId",
                table: "GameNotionPlayers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameNotionPlayers_Users_UserId",
                table: "GameNotionPlayers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameNotionPlayers_Users_UserId",
                table: "GameNotionPlayers");

            migrationBuilder.DropIndex(
                name: "IX_GameNotionPlayers_UserId",
                table: "GameNotionPlayers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GameNotionPlayers");
        }
    }
}
