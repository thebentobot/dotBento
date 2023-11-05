using System;
using System.Collections.Generic;

namespace dotBento.EntityFramework.Entities;

public partial class Guild
{
    public long GuildId { get; set; }

    public string GuildName { get; set; } = null!;

    public string Prefix { get; set; } = null!;

    public bool Tiktok { get; set; }

    public bool Leaderboard { get; set; }

    public bool Media { get; set; }

    public string? Icon { get; set; }

    public int? MemberCount { get; set; }

    public virtual ICollection<AnnouncementSchedule> AnnouncementSchedules { get; set; } = new List<AnnouncementSchedule>();

    public virtual ICollection<AnnouncementTime> AnnouncementTimes { get; set; } = new List<AnnouncementTime>();

    public virtual ICollection<AutoRole> AutoRoles { get; set; } = new List<AutoRole>();

    public virtual ICollection<AvailableRolesGuild> AvailableRolesGuilds { get; set; } = new List<AvailableRolesGuild>();

    public virtual ICollection<Ban> Bans { get; set; } = new List<Ban>();

    public virtual Bye? Bye { get; set; }

    public virtual CaseGlobal? CaseGlobal { get; set; }

    public virtual ICollection<ChannelDisable> ChannelDisables { get; set; } = new List<ChannelDisable>();

    public virtual ICollection<GuildMember> GuildMembers { get; set; } = new List<GuildMember>();

    public virtual ICollection<Kick> Kicks { get; set; } = new List<Kick>();

    public virtual MemberLog? MemberLog { get; set; }

    public virtual MessageLog? MessageLog { get; set; }

    public virtual ModLog? ModLog { get; set; }

    public virtual MuteRole? MuteRole { get; set; }

    public virtual ICollection<Mute> Mutes { get; set; } = new List<Mute>();

    public virtual RoleChannel? RoleChannel { get; set; }

    public virtual RoleMessage? RoleMessage { get; set; }

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public virtual ICollection<Warning> Warnings { get; set; } = new List<Warning>();

    public virtual Welcome? Welcome { get; set; }
}
