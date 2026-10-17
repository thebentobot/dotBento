using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotBento.EntityFramework.Migrations;

[DbContext(typeof(BotDbContext))]
[Migration("20260808000000_AddHoroscope")]
public partial class AddHoroscope : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "horoscope",
            columns: table => new
            {
                userID = table.Column<long>(type: "bigint", nullable: false),
                sign = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("horoscope_pk", x => x.userID);
                table.ForeignKey(
                    name: "horoscope_user_userid_fk",
                    column: x => x.userID,
                    principalTable: "user",
                    principalColumn: "userID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "horoscope_userid_uindex",
            table: "horoscope",
            column: "userID",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "horoscope");
    }
}
