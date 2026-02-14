using Microsoft.EntityFrameworkCore;
using dotBento.EntityFramework.Entities;
using Microsoft.Extensions.Configuration;

namespace dotBento.EntityFramework.Context;

public partial class BotDbContext : DbContext
{
    public virtual DbSet<Bento> Bentos { get; set; }

    public virtual DbSet<Guild> Guilds { get; set; }

    public virtual DbSet<GuildMember> GuildMembers { get; set; }

    public virtual DbSet<GuildSetting> GuildSettings { get; set; }

    public virtual DbSet<Lastfm> Lastfms { get; set; }

    public virtual DbSet<Patreon> Patreons { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<Reminder> Reminders { get; set; }

    public virtual DbSet<RpsGame> RpsGames { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSetting> UserSettings { get; set; }

    public virtual DbSet<Weather> Weathers { get; set; }
    
    private readonly IConfiguration _configuration;

    // Comment out below constructor when creating migrations locally
    public BotDbContext(IConfiguration configuration, DbContextOptions<BotDbContext> options)
        : base(options)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Comment out below connection string when creating migrations locally
            var connectionString = _configuration.GetConnectionString("PostgreSQL:ConnectionString") ?? throw new InvalidOperationException("PostgreSQL:ConnectionString environment variable are not set.");
            optionsBuilder.UseNpgsql(connectionString);

            // Uncomment below connection string when creating migrations, and also comment out the above iconfiguration stuff
            // optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=password;Database=bento;Command Timeout=60;Timeout=60;Persist Security Info=True");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bento>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("bento_pk");

            entity.ToTable("bento");

            entity.HasIndex(e => e.UserId, "bento_userid_uindex").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.Bento1).HasColumnName("bento");
            entity.Property(e => e.BentoDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("bentoDate");

            entity.HasOne(d => d.User).WithOne(p => p.Bento)
                .HasForeignKey<Bento>(d => d.UserId)
                .HasConstraintName("bento_user_userid_fk");
        });

        modelBuilder.Entity<Guild>(entity =>
        {
            entity.HasKey(e => e.GuildId).HasName("guild_pk");

            entity.ToTable("guild");

            entity.HasIndex(e => e.GuildId, "guild_guildid_uindex").IsUnique();

            entity.Property(e => e.GuildId)
                .ValueGeneratedNever()
                .HasColumnName("guildID");
            entity.Property(e => e.GuildName)
                .HasMaxLength(255)
                .HasColumnName("guildName");
            entity.Property(e => e.Icon)
                .HasColumnType("character varying")
                .HasColumnName("icon");
            entity.Property(e => e.Leaderboard).HasColumnName("leaderboard");
            entity.Property(e => e.Media).HasColumnName("media");
            entity.Property(e => e.MemberCount).HasColumnName("memberCount");
            entity.Property(e => e.Prefix)
                .HasMaxLength(16)
                .HasColumnName("prefix");
            entity.Property(e => e.Tiktok).HasColumnName("tiktok");
        });

        modelBuilder.Entity<GuildSetting>(entity =>
        {
            entity.HasKey(e => e.GuildId).HasName("guildsetting_pk");

            entity.ToTable("guildSetting");

            entity.HasIndex(e => e.GuildId, "guildsetting_guildid_uindex").IsUnique();

            entity.Property(e => e.GuildId)
                .ValueGeneratedNever()
                .HasColumnName("guildID");
            entity.Property(e => e.LeaderboardPublic)
                .HasDefaultValue(false)
                .HasColumnName("leaderboardPublic");

            entity.HasOne(d => d.Guild).WithOne(p => p.GuildSetting)
                .HasForeignKey<GuildSetting>(d => d.GuildId)
                .HasConstraintName("guildsetting_guild_guildid_fk");
        });

        modelBuilder.Entity<GuildMember>(entity =>
        {
            entity.HasKey(e => e.GuildMemberId).HasName("guildmember_pk");

            entity.ToTable("guildMember");

            entity.HasIndex(e => e.GuildMemberId, "guildmember_guildmemberid_uindex").IsUnique();

            entity.Property(e => e.GuildMemberId).HasColumnName("guildMemberID");
            entity.Property(e => e.AvatarUrl)
                .HasColumnType("character varying")
                .HasColumnName("avatarURL");
            entity.Property(e => e.GuildId).HasColumnName("guildID");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.UserId).HasColumnName("userID");
            entity.Property(e => e.Xp).HasColumnName("xp");

            entity.HasOne(d => d.Guild).WithMany(p => p.GuildMembers)
                .HasForeignKey(d => d.GuildId)
                .HasConstraintName("guildmember_guild_guildid_fk");

            entity.HasOne(d => d.User).WithMany(p => p.GuildMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("guildmember_user_userid_fk");
        });

        modelBuilder.Entity<Lastfm>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("lastfm_pk");

            entity.ToTable("lastfm");

            entity.HasIndex(e => e.UserId, "lastfm_userid_uindex").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.Lastfm1)
                .HasMaxLength(255)
                .HasColumnName("lastfm");

            entity.HasOne(d => d.User).WithOne(p => p.Lastfm)
                .HasForeignKey<Lastfm>(d => d.UserId)
                .HasConstraintName("lastfm_user_userid_fk");
        });

        modelBuilder.Entity<Patreon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("patreon_pk");

            entity.ToTable("patreon");

            entity.HasIndex(e => e.Id, "patreon_id_uindex").IsUnique();

            entity.HasIndex(e => e.UserId, "patreon_userid_uindex").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Avatar)
                .HasColumnType("character varying")
                .HasColumnName("avatar");
            entity.Property(e => e.Disciple).HasColumnName("disciple");
            entity.Property(e => e.EmoteSlot1)
                .HasColumnType("character varying")
                .HasColumnName("emoteSlot1");
            entity.Property(e => e.EmoteSlot2)
                .HasColumnType("character varying")
                .HasColumnName("emoteSlot2");
            entity.Property(e => e.EmoteSlot3)
                .HasColumnType("character varying")
                .HasColumnName("emoteSlot3");
            entity.Property(e => e.EmoteSlot4)
                .HasColumnType("character varying")
                .HasColumnName("emoteSlot4");
            entity.Property(e => e.Enthusiast).HasColumnName("enthusiast");
            entity.Property(e => e.Follower).HasColumnName("follower");
            entity.Property(e => e.Name)
                .HasColumnType("character varying")
                .HasColumnName("name");
            entity.Property(e => e.Sponsor).HasColumnName("sponsor");
            entity.Property(e => e.Supporter).HasColumnName("supporter");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.User).WithOne(p => p.Patreon)
                .HasForeignKey<Patreon>(d => d.UserId)
                .HasConstraintName("patreon_user_userid_fk");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("profile_pk");

            entity.ToTable("profile");

            entity.HasIndex(e => e.UserId, "profile_userid_uindex").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.BackgroundColour)
                .HasColumnType("character varying")
                .HasColumnName("backgroundColour");
            entity.Property(e => e.BackgroundUrl)
                .HasColumnType("character varying")
                .HasColumnName("backgroundUrl");
            entity.Property(e => e.Birthday)
                .HasColumnType("character varying")
                .HasColumnName("birthday");
            entity.Property(e => e.Description)
                .HasColumnType("character varying")
                .HasColumnName("description");
            entity.Property(e => e.DescriptionColour)
                .HasColumnType("character varying")
                .HasColumnName("descriptionColour");
            entity.Property(e => e.DescriptionColourOpacity).HasColumnName("descriptionColourOpacity");
            entity.Property(e => e.DiscriminatorColour)
                .HasColumnType("character varying")
                .HasColumnName("discriminatorColour");
            entity.Property(e => e.FmArtistTextColour)
                .HasColumnType("character varying")
                .HasColumnName("fmArtistTextColour");
            entity.Property(e => e.FmArtistTextOpacity).HasColumnName("fmArtistTextOpacity");
            entity.Property(e => e.FmDivBgcolour)
                .HasColumnType("character varying")
                .HasColumnName("fmDivBGColour");
            entity.Property(e => e.FmDivBgopacity).HasColumnName("fmDivBGOpacity");
            entity.Property(e => e.FmSongTextColour)
                .HasColumnType("character varying")
                .HasColumnName("fmSongTextColour");
            entity.Property(e => e.FmSongTextOpacity).HasColumnName("fmSongTextOpacity");
            entity.Property(e => e.LastfmBoard).HasColumnName("lastfmBoard");
            entity.Property(e => e.OverlayColour)
                .HasColumnType("character varying")
                .HasColumnName("overlayColour");
            entity.Property(e => e.OverlayOpacity).HasColumnName("overlayOpacity");
            entity.Property(e => e.SidebarBlur).HasColumnName("sidebarBlur");
            entity.Property(e => e.SidebarColour)
                .HasColumnType("character varying")
                .HasColumnName("sidebarColour");
            entity.Property(e => e.SidebarItemBentoColour)
                .HasColumnType("character varying")
                .HasColumnName("sidebarItemBentoColour");
            entity.Property(e => e.SidebarItemGlobalColour)
                .HasColumnType("character varying")
                .HasColumnName("sidebarItemGlobalColour");
            entity.Property(e => e.SidebarItemServerColour)
                .HasColumnType("character varying")
                .HasColumnName("sidebarItemServerColour");
            entity.Property(e => e.SidebarItemTimezoneColour)
                .HasColumnType("character varying")
                .HasColumnName("sidebarItemTimezoneColour");
            entity.Property(e => e.SidebarOpacity).HasColumnName("sidebarOpacity");
            entity.Property(e => e.SidebarValueBentoColour)
                .HasColumnType("character varying")
                .HasColumnName("sidebarValueBentoColour");
            entity.Property(e => e.SidebarValueGlobalColour)
                .HasColumnType("character varying")
                .HasColumnName("sidebarValueGlobalColour");
            entity.Property(e => e.SidebarValueServerColour)
                .HasColumnType("character varying")
                .HasColumnName("sidebarValueServerColour");
            entity.Property(e => e.Timezone)
                .HasColumnType("character varying")
                .HasColumnName("timezone");
            entity.Property(e => e.UsernameColour)
                .HasColumnType("character varying")
                .HasColumnName("usernameColour");
            entity.Property(e => e.XpBar2Colour)
                .HasColumnType("character varying")
                .HasColumnName("xpBar2Colour");
            entity.Property(e => e.XpBar2Opacity).HasColumnName("xpBar2Opacity");
            entity.Property(e => e.XpBarColour)
                .HasColumnType("character varying")
                .HasColumnName("xpBarColour");
            entity.Property(e => e.XpBarOpacity).HasColumnName("xpBarOpacity");
            entity.Property(e => e.XpBoard).HasColumnName("xpBoard");
            entity.Property(e => e.XpDivBgcolour)
                .HasColumnType("character varying")
                .HasColumnName("xpDivBGColour");
            entity.Property(e => e.XpDivBgopacity).HasColumnName("xpDivBGOpacity");
            entity.Property(e => e.XpDoneGlobalColour1)
                .HasColumnType("character varying")
                .HasColumnName("xpDoneGlobalColour1");
            entity.Property(e => e.XpDoneGlobalColour1Opacity).HasColumnName("xpDoneGlobalColour1Opacity");
            entity.Property(e => e.XpDoneGlobalColour2)
                .HasColumnType("character varying")
                .HasColumnName("xpDoneGlobalColour2");
            entity.Property(e => e.XpDoneGlobalColour2Opacity).HasColumnName("xpDoneGlobalColour2Opacity");
            entity.Property(e => e.XpDoneGlobalColour3)
                .HasColumnType("character varying")
                .HasColumnName("xpDoneGlobalColour3");
            entity.Property(e => e.XpDoneGlobalColour3Opacity).HasColumnName("xpDoneGlobalColour3Opacity");
            entity.Property(e => e.XpDoneServerColour1)
                .HasColumnType("character varying")
                .HasColumnName("xpDoneServerColour1");
            entity.Property(e => e.XpDoneServerColour1Opacity).HasColumnName("xpDoneServerColour1Opacity");
            entity.Property(e => e.XpDoneServerColour2)
                .HasColumnType("character varying")
                .HasColumnName("xpDoneServerColour2");
            entity.Property(e => e.XpDoneServerColour2Opacity).HasColumnName("xpDoneServerColour2Opacity");
            entity.Property(e => e.XpDoneServerColour3)
                .HasColumnType("character varying")
                .HasColumnName("xpDoneServerColour3");
            entity.Property(e => e.XpDoneServerColour3Opacity).HasColumnName("xpDoneServerColour3Opacity");
            entity.Property(e => e.XpText2Colour)
                .HasColumnType("character varying")
                .HasColumnName("xpText2Colour");
            entity.Property(e => e.XpText2Opacity).HasColumnName("xpText2Opacity");
            entity.Property(e => e.XpTextColour)
                .HasColumnType("character varying")
                .HasColumnName("xpTextColour");
            entity.Property(e => e.XpTextOpacity).HasColumnName("xpTextOpacity");

            entity.HasOne(d => d.User).WithOne(p => p.Profile)
                .HasForeignKey<Profile>(d => d.UserId)
                .HasConstraintName("profile_user_userid_fk");
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reminder_pk");

            entity.ToTable("reminder");

            entity.HasIndex(e => e.Id, "reminder_id_uindex").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.Reminder1)
                .HasColumnType("character varying")
                .HasColumnName("reminder");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.User).WithMany(p => p.Reminders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("reminder_user_userid_fk");
        });

        modelBuilder.Entity<RpsGame>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rpsgame_pk");

            entity.ToTable("rpsGame");

            entity.HasIndex(e => e.Id, "rpsgame_id_uindex").IsUnique();

            entity.HasIndex(e => e.UserId, "rpsgame_userid_uindex").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PaperLosses).HasColumnName("paperLosses");
            entity.Property(e => e.PaperTies).HasColumnName("paperTies");
            entity.Property(e => e.PaperWins).HasColumnName("paperWins");
            entity.Property(e => e.RockLosses).HasColumnName("rockLosses");
            entity.Property(e => e.RockTies).HasColumnName("rockTies");
            entity.Property(e => e.RockWins).HasColumnName("rockWins");
            entity.Property(e => e.ScissorWins).HasColumnName("scissorWins");
            entity.Property(e => e.ScissorsLosses).HasColumnName("scissorsLosses");
            entity.Property(e => e.ScissorsTies).HasColumnName("scissorsTies");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.User).WithOne(p => p.RpsGame)
                .HasForeignKey<RpsGame>(d => d.UserId)
                .HasConstraintName("rpsgame_user_userid_fk");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("tag_pk");

            entity.ToTable("tag");

            entity.HasIndex(e => e.TagId, "tag_tagid_uindex").IsUnique();

            entity.Property(e => e.TagId).HasColumnName("tagID");
            entity.Property(e => e.Command)
                .HasMaxLength(255)
                .HasColumnName("command");
            entity.Property(e => e.Content)
                .HasColumnType("character varying")
                .HasColumnName("content");
            entity.Property(e => e.Count).HasColumnName("count");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("now()")
                .HasColumnName("date");
            entity.Property(e => e.GuildId).HasColumnName("guildID");
            entity.Property(e => e.UserId).HasColumnName("userID");

            entity.HasOne(d => d.Guild).WithMany(p => p.Tags)
                .HasForeignKey(d => d.GuildId)
                .HasConstraintName("tag_guild_guildid_fk");

            entity.HasOne(d => d.User).WithMany(p => p.Tags)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("tag_user_userid_fk");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_pk");

            entity.ToTable("user");

            entity.HasIndex(e => e.UserId, "user_userid_uindex").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.AvatarUrl)
                .HasColumnType("character varying")
                .HasColumnName("avatarURL");
            entity.Property(e => e.Discriminator)
                .HasColumnType("character varying")
                .HasColumnName("discriminator");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.Username)
                .HasColumnType("character varying")
                .HasColumnName("username");
            entity.Property(e => e.Xp).HasColumnName("xp");
        });

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("usersetting_pk");

            entity.ToTable("userSetting");

            entity.HasIndex(e => e.UserId, "usersetting_userid_uindex").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.HideSlashCommandCalls)
                .HasDefaultValue(false)
                .HasColumnName("hideSlashCommandCalls");
            entity.Property(e => e.ShowOnGlobalLeaderboard)
                .HasDefaultValue(true)
                .HasColumnName("showOnGlobalLeaderboard");

            entity.HasOne(d => d.User).WithOne(p => p.UserSetting)
                .HasForeignKey<UserSetting>(d => d.UserId)
                .HasConstraintName("usersetting_user_userid_fk");
        });

        modelBuilder.Entity<Weather>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("weather_pk");

            entity.ToTable("weather");

            entity.HasIndex(e => e.UserId, "weather_userid_uindex").IsUnique();

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("userID");
            entity.Property(e => e.City)
                .HasMaxLength(255)
                .HasColumnName("city");

            entity.HasOne(d => d.User).WithOne(p => p.Weather)
                .HasForeignKey<Weather>(d => d.UserId)
                .HasConstraintName("weather_user_userid_fk");
        });
        modelBuilder.HasSequence("bento_bentoDate_seq");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
