using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace dotBento.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:horos", "Aries,Taurus,Gemini,Cancer,Leo,Virgo,Libra,Scorpio,Sagittarius,Capricorn,Aquarius,Pisces")
                .Annotation("Npgsql:Enum:roletypes", "main,sub,other");

            migrationBuilder.CreateSequence(
                name: "bento_bentoDate_seq");

            migrationBuilder.CreateTable(
                name: "_prisma_migrations",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    migration_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    logs = table.Column<string>(type: "text", nullable: true),
                    rolled_back_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    applied_steps_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("_prisma_migrations_pkey", x => x.id);
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
                    messageId = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
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
                name: "guild",
                columns: table => new
                {
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    guildName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    tiktok = table.Column<bool>(type: "boolean", nullable: false),
                    leaderboard = table.Column<bool>(type: "boolean", nullable: false),
                    media = table.Column<bool>(type: "boolean", nullable: false),
                    icon = table.Column<string>(type: "character varying", nullable: true),
                    memberCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("guild_pk", x => x.guildID);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    discriminator = table.Column<string>(type: "character varying", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "character varying", nullable: true),
                    avatarURL = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_pk", x => x.userID);
                });

            migrationBuilder.CreateTable(
                name: "announcementSchedule",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    channelID = table.Column<long>(type: "bigint", nullable: false),
                    message = table.Column<string>(type: "character varying", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    channelID = table.Column<long>(type: "bigint", nullable: false),
                    message = table.Column<string>(type: "character varying", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amountOfTime = table.Column<int>(type: "integer", nullable: false),
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
                    role = table.Column<string>(type: "character varying", nullable: false),
                    guildID = table.Column<long>(type: "bigint", nullable: false)
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
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    note = table.Column<string>(type: "character varying", nullable: true),
                    actor = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "character varying", nullable: true)
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
                    message = table.Column<string>(type: "character varying", nullable: true),
                    channel = table.Column<long>(type: "bigint", nullable: true)
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
                    serverName = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "kick",
                columns: table => new
                {
                    kickCase = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    note = table.Column<string>(type: "character varying", nullable: true),
                    actor = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "character varying", nullable: true)
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
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    muteEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying", nullable: true),
                    actor = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "character varying", nullable: true),
                    MuteStatus = table.Column<bool>(type: "boolean", nullable: false),
                    NonBentoMute = table.Column<bool>(type: "boolean", nullable: true)
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
                name: "role",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    roleID = table.Column<long>(type: "bigint", nullable: false),
                    roleCommand = table.Column<string>(type: "character varying", nullable: false),
                    roleName = table.Column<string>(type: "character varying", nullable: true),
                    guildID = table.Column<long>(type: "bigint", nullable: false)
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
                    messageID = table.Column<long>(type: "bigint", nullable: true),
                    message = table.Column<string>(type: "character varying", nullable: true)
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
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    note = table.Column<string>(type: "character varying", nullable: true),
                    actor = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying", nullable: true)
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
                    message = table.Column<string>(type: "character varying", nullable: true),
                    channel = table.Column<long>(type: "bigint", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "bento",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    bento = table.Column<int>(type: "integer", nullable: false),
                    bentoDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("bento_pk", x => x.userID);
                    table.ForeignKey(
                        name: "bento_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guildMember",
                columns: table => new
                {
                    guildMemberID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    avatarURL = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("guildmember_pk", x => x.guildMemberID);
                    table.ForeignKey(
                        name: "guildmember_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "guildmember_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "horoscope",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false)
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
                name: "lastfm",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    lastfm = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("lastfm_pk", x => x.userID);
                    table.ForeignKey(
                        name: "lastfm_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notificationMessage",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "character varying", nullable: false),
                    global = table.Column<bool>(type: "boolean", nullable: true)
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
                name: "patreon",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying", nullable: true),
                    avatar = table.Column<string>(type: "character varying", nullable: true),
                    supporter = table.Column<bool>(type: "boolean", nullable: false),
                    follower = table.Column<bool>(type: "boolean", nullable: false),
                    enthusiast = table.Column<bool>(type: "boolean", nullable: false),
                    disciple = table.Column<bool>(type: "boolean", nullable: false),
                    sponsor = table.Column<bool>(type: "boolean", nullable: false),
                    emoteSlot1 = table.Column<string>(type: "character varying", nullable: true),
                    emoteSlot2 = table.Column<string>(type: "character varying", nullable: true),
                    emoteSlot3 = table.Column<string>(type: "character varying", nullable: true),
                    emoteSlot4 = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("patreon_pk", x => x.id);
                    table.ForeignKey(
                        name: "patreon_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    lastfmBoard = table.Column<bool>(type: "boolean", nullable: true),
                    xpBoard = table.Column<bool>(type: "boolean", nullable: true),
                    backgroundUrl = table.Column<string>(type: "character varying", nullable: true),
                    BackgroundColourOpacity = table.Column<int>(type: "integer", nullable: true),
                    backgroundColour = table.Column<string>(type: "character varying", nullable: true),
                    descriptionColourOpacity = table.Column<int>(type: "integer", nullable: true),
                    descriptionColour = table.Column<string>(type: "character varying", nullable: true),
                    overlayOpacity = table.Column<int>(type: "integer", nullable: true),
                    overlayColour = table.Column<string>(type: "character varying", nullable: true),
                    usernameColour = table.Column<string>(type: "character varying", nullable: true),
                    discriminatorColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarItemServerColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarItemGlobalColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarItemBentoColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarItemTimezoneColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarValueServerColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarValueGlobalColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarValueBentoColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarOpacity = table.Column<int>(type: "integer", nullable: true),
                    sidebarColour = table.Column<string>(type: "character varying", nullable: true),
                    sidebarBlur = table.Column<int>(type: "integer", nullable: true),
                    fmDivBGOpacity = table.Column<int>(type: "integer", nullable: true),
                    fmDivBGColour = table.Column<string>(type: "character varying", nullable: true),
                    fmSongTextOpacity = table.Column<int>(type: "integer", nullable: true),
                    fmSongTextColour = table.Column<string>(type: "character varying", nullable: true),
                    fmArtistTextOpacity = table.Column<int>(type: "integer", nullable: true),
                    fmArtistTextColour = table.Column<string>(type: "character varying", nullable: true),
                    xpDivBGOpacity = table.Column<int>(type: "integer", nullable: true),
                    xpDivBGColour = table.Column<string>(type: "character varying", nullable: true),
                    xpTextOpacity = table.Column<int>(type: "integer", nullable: true),
                    xpTextColour = table.Column<string>(type: "character varying", nullable: true),
                    xpText2Opacity = table.Column<int>(type: "integer", nullable: true),
                    xpText2Colour = table.Column<string>(type: "character varying", nullable: true),
                    xpDoneServerColour1Opacity = table.Column<int>(type: "integer", nullable: true),
                    xpDoneServerColour1 = table.Column<string>(type: "character varying", nullable: true),
                    xpDoneServerColour2Opacity = table.Column<int>(type: "integer", nullable: true),
                    xpDoneServerColour2 = table.Column<string>(type: "character varying", nullable: true),
                    xpDoneServerColour3Opacity = table.Column<int>(type: "integer", nullable: true),
                    xpDoneServerColour3 = table.Column<string>(type: "character varying", nullable: true),
                    xpDoneGlobalColour1Opacity = table.Column<int>(type: "integer", nullable: true),
                    xpDoneGlobalColour1 = table.Column<string>(type: "character varying", nullable: true),
                    xpDoneGlobalColour2Opacity = table.Column<int>(type: "integer", nullable: true),
                    xpDoneGlobalColour2 = table.Column<string>(type: "character varying", nullable: true),
                    xpDoneGlobalColour3Opacity = table.Column<int>(type: "integer", nullable: true),
                    xpDoneGlobalColour3 = table.Column<string>(type: "character varying", nullable: true),
                    description = table.Column<string>(type: "character varying", nullable: true),
                    timezone = table.Column<string>(type: "character varying", nullable: true),
                    birthday = table.Column<string>(type: "character varying", nullable: true),
                    xpBarOpacity = table.Column<int>(type: "integer", nullable: true),
                    xpBarColour = table.Column<string>(type: "character varying", nullable: true),
                    xpBar2Opacity = table.Column<int>(type: "integer", nullable: true),
                    xpBar2Colour = table.Column<string>(type: "character varying", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("profile_pk", x => x.userID);
                    table.ForeignKey(
                        name: "profile_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reminder",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    reminder = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("reminder_pk", x => x.id);
                    table.ForeignKey(
                        name: "reminder_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rpsGame",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    paperWins = table.Column<int>(type: "integer", nullable: true),
                    paperLosses = table.Column<int>(type: "integer", nullable: true),
                    rockWins = table.Column<int>(type: "integer", nullable: true),
                    rockLosses = table.Column<int>(type: "integer", nullable: true),
                    scissorWins = table.Column<int>(type: "integer", nullable: true),
                    scissorsLosses = table.Column<int>(type: "integer", nullable: true),
                    paperTies = table.Column<int>(type: "integer", nullable: true),
                    rockTies = table.Column<int>(type: "integer", nullable: true),
                    scissorsTies = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("rpsgame_pk", x => x.id);
                    table.ForeignKey(
                        name: "rpsgame_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tag",
                columns: table => new
                {
                    tagID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    guildID = table.Column<long>(type: "bigint", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    command = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content = table.Column<string>(type: "character varying", nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tag_pk", x => x.tagID);
                    table.ForeignKey(
                        name: "tag_guild_guildid_fk",
                        column: x => x.guildID,
                        principalTable: "guild",
                        principalColumn: "guildID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "tag_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weather",
                columns: table => new
                {
                    userID = table.Column<long>(type: "bigint", nullable: false),
                    city = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("weather_pk", x => x.userID);
                    table.ForeignKey(
                        name: "weather_user_userid_fk",
                        column: x => x.userID,
                        principalTable: "user",
                        principalColumn: "userID",
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
                name: "bento_userid_uindex",
                table: "bento",
                column: "userID",
                unique: true);

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
                name: "guild_guildid_uindex",
                table: "guild",
                column: "guildID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "guildmember_guildmemberid_uindex",
                table: "guildMember",
                column: "guildMemberID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guildMember_guildID",
                table: "guildMember",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "IX_guildMember_userID",
                table: "guildMember",
                column: "userID");

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
                name: "lastfm_userid_uindex",
                table: "lastfm",
                column: "userID",
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
                name: "patreon_id_uindex",
                table: "patreon",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "patreon_userid_uindex",
                table: "patreon",
                column: "userID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "profile_userid_uindex",
                table: "profile",
                column: "userID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reminder_userID",
                table: "reminder",
                column: "userID");

            migrationBuilder.CreateIndex(
                name: "reminder_id_uindex",
                table: "reminder",
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
                name: "rpsgame_id_uindex",
                table: "rpsGame",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "rpsgame_userid_uindex",
                table: "rpsGame",
                column: "userID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tag_guildID",
                table: "tag",
                column: "guildID");

            migrationBuilder.CreateIndex(
                name: "IX_tag_userID",
                table: "tag",
                column: "userID");

            migrationBuilder.CreateIndex(
                name: "tag_tagid_uindex",
                table: "tag",
                column: "tagID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "user_userid_uindex",
                table: "user",
                column: "userID",
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
                name: "weather_userid_uindex",
                table: "weather",
                column: "userID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "welcome_guildid_uindex",
                table: "welcome",
                column: "guildID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "_prisma_migrations");

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
                name: "bento");

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
                name: "guildMember");

            migrationBuilder.DropTable(
                name: "horoscope");

            migrationBuilder.DropTable(
                name: "kick");

            migrationBuilder.DropTable(
                name: "lastfm");

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
                name: "patreon");

            migrationBuilder.DropTable(
                name: "profile");

            migrationBuilder.DropTable(
                name: "reminder");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "roleChannel");

            migrationBuilder.DropTable(
                name: "roleMessages");

            migrationBuilder.DropTable(
                name: "rpsGame");

            migrationBuilder.DropTable(
                name: "tag");

            migrationBuilder.DropTable(
                name: "warning");

            migrationBuilder.DropTable(
                name: "weather");

            migrationBuilder.DropTable(
                name: "welcome");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "guild");

            migrationBuilder.DropSequence(
                name: "bento_bentoDate_seq");
        }
    }
}
