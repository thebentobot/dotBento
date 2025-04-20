using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace dotBento.EntityFramework.Migrations.Temp
{
    /// <inheritdoc />
    public partial class RemoveUnusedTablesAfterDotnetMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcementSchedule");

            migrationBuilder.DropTable(
                name: "announcementTime");

            migrationBuilder.DropTable(
                name: "autoRole");

            migrationBuilder.DropTable(
                name: "availableRolesGuild");

            migrationBuilder.DropTable(
                name: "ban");

            migrationBuilder.DropTable(
                name: "bye");

            migrationBuilder.DropTable(
                name: "caseGlobal");

            migrationBuilder.DropTable(
                name: "channelDisable");

            migrationBuilder.DropTable(
                name: "gfycatBlacklist");

            migrationBuilder.DropTable(
                name: "gfycatPosts");

            migrationBuilder.DropTable(
                name: "gfycatWordList");

            migrationBuilder.DropTable(
                name: "horoscope");

            migrationBuilder.DropTable(
                name: "kick");

            migrationBuilder.DropTable(
                name: "memberLog");

            migrationBuilder.DropTable(
                name: "messageLog");

            migrationBuilder.DropTable(
                name: "modLog");

            migrationBuilder.DropTable(
                name: "mute");

            migrationBuilder.DropTable(
                name: "muteRole");

            migrationBuilder.DropTable(
                name: "notificationMessage");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "roleChannel");

            migrationBuilder.DropTable(
                name: "roleMessages");

            migrationBuilder.DropTable(
                name: "warning");

            migrationBuilder.DropTable(
                name: "welcome");
            
            // Drop unused enum types manually
            migrationBuilder.Sql(@"DROP TYPE IF EXISTS horos;");
            migrationBuilder.Sql(@"DROP TYPE IF EXISTS roletypes;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE TYPE horos AS ENUM ('Aries', 'Taurus', 'Gemini', 'Cancer', 'Leo', 'Virgo', 'Libra', 'Scorpio', 'Sagittarius', 'Capricorn', 'Aquarius', 'Pisces');");
            migrationBuilder.Sql(@"CREATE TYPE roletypes AS ENUM ('main', 'sub', 'other');");
            
            migrationBuilder.CreateTable(
                name: "announcementSchedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channelID = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("announcementschedule_pk", x => x.id);
                    table.ForeignKey(
                        name: "announcementschedule_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "announcementTime",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    amountOfTime = table.Column<int>(type: "integer", nullable: false),
                    channelID = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "character varying", nullable: false),
                    timeframe = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("announcementtime_pk", x => x.id);
                    table.ForeignKey(
                        name: "announcementtime_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "autoRole",
                columns: table => new
                {
                    autoRoleID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    roleID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("autorole_pk", x => x.autoRoleID);
                    table.ForeignKey(
                        name: "autorole_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availableRolesGuild",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<string>(type: "character varying", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("availablerolesguild_pk", x => x.id);
                    table.ForeignKey(
                        name: "availablerolesguild_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ban",
                columns: table => new
                {
                    banCase = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    actor = table.Column<long>(type: "bigint", nullable: true),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    note = table.Column<string>(type: "character varying", nullable: true),
                    reason = table.Column<string>(type: "character varying", nullable: true),
                    userID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ban_pk", x => x.banCase);
                    table.ForeignKey(
                        name: "ban_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bye",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channel = table.Column<long>(type: "bigint", nullable: true),
                    message = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("bye_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "bye_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "caseGlobal",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<bool>(type: "boolean", nullable: false),
                    serverName = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("caseglobal_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "caseglobal_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "channelDisable",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channelID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("channeldisable_pk", x => x.id);
                    table.ForeignKey(
                        name: "channeldisable_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gfycatBlacklist",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("gfycatblacklist_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gfycatPosts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    content = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    messageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("gfycatposts_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gfycatWordList",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    word = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("gfycatwordlist_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "horoscope",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    horoscope = table.Column<string>(type: "text", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "kick",
                columns: table => new
                {
                    kickCase = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    actor = table.Column<long>(type: "bigint", nullable: true),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    note = table.Column<string>(type: "character varying", nullable: true),
                    reason = table.Column<string>(type: "character varying", nullable: true),
                    userID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("kick_pk", x => x.kickCase);
                    table.ForeignKey(
                        name: "kick_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "memberLog",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channel = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("memberlog_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "memberlog_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "messageLog",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channel = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("messagelog_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "messagelog_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "modLog",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channel = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("modlog_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "modlog_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mute",
                columns: table => new
                {
                    muteCase = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    actor = table.Column<long>(type: "bigint", nullable: true),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    muteEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MuteStatus = table.Column<bool>(type: "boolean", nullable: false),
                    NonBentoMute = table.Column<bool>(type: "boolean", nullable: true),
                    note = table.Column<string>(type: "character varying", nullable: true),
                    reason = table.Column<string>(type: "character varying", nullable: true),
                    userID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("mute_pk", x => x.muteCase);
                    table.ForeignKey(
                        name: "mute_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "muteRole",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    roleID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("muterole_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "muterole_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notificationMessage",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "character varying", nullable: false),
                    global = table.Column<bool>(type: "boolean", nullable: true),
                    guildID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notificationmessage_pk", x => x.id);
                    table.ForeignKey(
                        name: "notificationmessage_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    roleCommand = table.Column<string>(type: "character varying", nullable: false),
                    roleID = table.Column<long>(type: "bigint", nullable: false),
                    roleName = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("role_pk", x => x.id);
                    table.ForeignKey(
                        name: "role_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roleChannel",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channelID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("rolechannel_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "rolechannel_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roleMessages",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    message = table.Column<string>(type: "character varying", nullable: true),
                    messageID = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("rolemessages_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "rolemessages_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "warning",
                columns: table => new
                {
                    warningCase = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    actor = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    note = table.Column<string>(type: "character varying", nullable: true),
                    reason = table.Column<string>(type: "character varying", nullable: true),
                    userID = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("warning_pk", x => x.warningCase);
                    table.ForeignKey(
                        name: "warning_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "welcome",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channel = table.Column<long>(type: "bigint", nullable: true),
                    message = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("welcome_pk", x => x.guildID);
                    table.ForeignKey(
                        name: "welcome_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "announcementschedule_id_uindex",
                table: "announcementSchedule",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_announcementSchedule_guildID",
                table: "announcementSchedule",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "announcementtime_id_uindex",
                table: "announcementTime",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_announcementTime_guildID",
                table: "announcementTime",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "autorole_autoroleid_uindex",
                table: "autoRole",
                column: "autoRoleID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_autoRole_guildID",
                table: "autoRole",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "availablerolesguild_id_uindex",
                table: "availableRolesGuild",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_availableRolesGuild_guildID",
                table: "availableRolesGuild",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "ban_mutecase_uindex",
                table: "ban",
                column: "banCase",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ban_guildID",
                table: "ban",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "bye_guildid_uindex",
                table: "bye",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "caseglobal_guildid_uindex",
                table: "caseGlobal",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "channeldisable_channelid_uindex",
                table: "channelDisable",
                column: "channelID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "channeldisable_id_uindex",
                table: "channelDisable",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channelDisable_guildID",
                table: "channelDisable",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "gfycatblacklist_id_uindex",
                table: "gfycatBlacklist",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "gfycatblacklist_username_uindex",
                table: "gfycatBlacklist",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "gfycatposts_id_uindex",
                table: "gfycatPosts",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "gfycatwordlist_id_uindex",
                table: "gfycatWordList",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "gfycatwordlist_word_uindex",
                table: "gfycatWordList",
                column: "word",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "horoscope_userid_uindex",
                table: "horoscope",
                column: "userID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kick_guildID",
                table: "kick",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "kick_mutecase_uindex",
                table: "kick",
                column: "kickCase",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "memberlog_channel_uindex",
                table: "memberLog",
                column: "channel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "memberlog_guildid_uindex",
                table: "memberLog",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "messagelog_guildid_uindex",
                table: "messageLog",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "modlog_guildid_uindex",
                table: "modLog",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mute_guildID",
                table: "mute",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "mute_mutecase_uindex",
                table: "mute",
                column: "muteCase",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "muterole_guildid_uindex",
                table: "muteRole",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "muterole_role_uindex",
                table: "muteRole",
                column: "roleID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notificationMessage_userID",
                table: "notificationMessage",
                column: "userID");

            migrationBuilder.CreateIndex(
                name: "notificationmessage_id_uindex",
                table: "notificationMessage",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_guildID",
                table: "role",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "role_id_uindex",
                table: "role",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "rolechannel_channelid_uindex",
                table: "roleChannel",
                column: "channelID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "rolechannel_guildid_uindex",
                table: "roleChannel",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warning_guildID",
                table: "warning",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "warning_mutecase_uindex",
                table: "warning",
                column: "warningCase",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "welcome_guildid_uindex",
                table: "welcome",
                column: "guildID",
                unique: true);
        }
    }
}
