using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diguifi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBundleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BundleType",
                table: "Bundles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BundleType",
                table: "Bundles");
        }
    }
}
