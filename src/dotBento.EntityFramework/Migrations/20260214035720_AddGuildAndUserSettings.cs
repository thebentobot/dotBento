using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotBento.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildAndUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "guildSetting",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    leaderboardPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("guildsetting_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "guildsetting_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "userSetting",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    hideSlashCommandCalls = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    showOnGlobalLeaderboard = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("usersetting_pk", x => x.userID);
                    table.ForeignKey(
                        name: "usersetting_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "guildsetting_guildid_uindex",
                table: "guildSetting",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "usersetting_userid_uindex",
                table: "userSetting",
                column: "userID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "guildSetting");

            migrationBuilder.DropTable(
                name: "userSetting");
        }
    }
}
