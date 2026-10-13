using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManufacturingToolTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddToolRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "Tools",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Tools");
        }
    }
}
