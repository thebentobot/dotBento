using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotBento.EntityFramework.Migrations.Temp
{
    /// <inheritdoc />
    public partial class TempCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:Enum:horos", "Aries,Taurus,Gemini,Cancer,Leo,Virgo,Libra,Scorpio,Sagittarius,Capricorn,Aquarius,Pisces")
                .OldAnnotation("Npgsql:Enum:roletypes", "main,sub,other");

            migrationBuilder.AddColumn<string>(
                name: "horoscope",
                table: "horoscope",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "availableRolesGuild",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "horoscope",
                table: "horoscope");

            migrationBuilder.DropColumn(
                name: "type",
                table: "availableRolesGuild");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:horos", "Aries,Taurus,Gemini,Cancer,Leo,Virgo,Libra,Scorpio,Sagittarius,Capricorn,Aquarius,Pisces")
                .Annotation("Npgsql:Enum:roletypes", "main,sub,other");
        }
    }
}
