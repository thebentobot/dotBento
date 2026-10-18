using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotBento.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOptOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "userOptOut",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    optedOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("useroptout_pk", x => x.userID);
                });

            migrationBuilder.CreateIndex(
                name: "useroptout_userid_uindex",
                table: "userOptOut",
                column: "userID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "userOptOut");
        }
    }
}
