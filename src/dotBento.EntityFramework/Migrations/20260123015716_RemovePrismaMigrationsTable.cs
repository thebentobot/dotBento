using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotBento.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class RemovePrismaMigrationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_prisma_migrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "_prisma_migrations",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    applied_steps_count = table.Column<int>(type: "integer", nullable: false),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    logs = table.Column<string>(type: "text", nullable: true),
                    migration_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    rolled_back_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("_prisma_migrations_pkey", x => x.id);
                });
        }
    }
}
