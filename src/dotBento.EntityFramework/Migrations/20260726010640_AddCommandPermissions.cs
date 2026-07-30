using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotBento.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "adminOnlyCommands",
                table: "guildSetting",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "disabledCommands",
                table: "guildSetting",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "adminOnlyCommands",
                table: "guildSetting");

            migrationBuilder.DropColumn(
                name: "disabledCommands",
                table: "guildSetting");
        }
    }
}
