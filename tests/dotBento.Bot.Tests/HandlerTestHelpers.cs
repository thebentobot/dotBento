using System.Reflection;
using System.Runtime.CompilerServices;
using dotBento.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NetCord;
using NetCord.Gateway;

namespace dotBento.Bot.Tests;

internal sealed class InMemoryDbFactory(string? dbName = null, InMemoryDatabaseRoot? root = null)
    : IDbContextFactory<BotDbContext>
{
    private readonly string _dbName = dbName ?? Guid.NewGuid().ToString("N");
    private readonly InMemoryDatabaseRoot _root = root ?? new InMemoryDatabaseRoot();
    private readonly IConfiguration _config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>())
        .Build();

    public BotDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(_dbName, _root)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new BotDbContext(_config, options);
    }

    public Task<BotDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}

internal static class NetCordFakes
{
    private static readonly Assembly NetCordAsm = typeof(GuildUser).Assembly;

    private static void SetDeclaredField(object obj, Type declaringType, string fieldName, object? value)
    {
        var field = declaringType.GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
            ?? throw new MissingFieldException(declaringType.FullName, fieldName);
        field.SetValue(obj, value);
    }

    private static object CreateUninitialized(string typeName)
    {
        var type = NetCordAsm.GetType(typeName) ?? throw new TypeLoadException(typeName);
        return RuntimeHelpers.GetUninitializedObject(type);
    }

    public static GuildUser CreateGuildUser(
        ulong id,
        ulong guildId,
        bool isBot = false,
        string username = "testuser",
        string? avatarHash = null,
        string? guildAvatarHash = null)
    {
        var jsonEntityType = NetCordAsm.GetType("NetCord.JsonModels.JsonEntity")!;
        var jsonUserType = NetCordAsm.GetType("NetCord.JsonModels.JsonUser")!;
        var jsonGuildUserType = NetCordAsm.GetType("NetCord.JsonModels.JsonGuildUser")!;
        var partialGuildUserType = NetCordAsm.GetType("NetCord.PartialGuildUser")!;

        var jsonUser = CreateUninitialized("NetCord.JsonModels.JsonUser");
        SetDeclaredField(jsonUser, jsonEntityType, "<Id>k__BackingField", id);
        SetDeclaredField(jsonUser, jsonUserType, "<IsBot>k__BackingField", isBot);
        SetDeclaredField(jsonUser, jsonUserType, "<Username>k__BackingField", username);
        SetDeclaredField(jsonUser, jsonUserType, "<AvatarHash>k__BackingField", avatarHash);

        var jsonGuildUser = CreateUninitialized("NetCord.JsonModels.JsonGuildUser");
        SetDeclaredField(jsonGuildUser, jsonGuildUserType, "<GuildAvatarHash>k__BackingField", guildAvatarHash);
        SetDeclaredField(jsonGuildUser, jsonGuildUserType, "<User>k__BackingField", jsonUser);

        var guildUser = (GuildUser)RuntimeHelpers.GetUninitializedObject(typeof(GuildUser));
        SetDeclaredField(guildUser, typeof(User), "_jsonModel", jsonUser);
        SetDeclaredField(guildUser, partialGuildUserType, "_jsonModel", jsonGuildUser);
        SetDeclaredField(guildUser, typeof(GuildUser), "<guildId>P", guildId);

        return guildUser;
    }

    public static Guild CreateGatewayGuild(
        ulong id,
        string name,
        ulong ownerId = 0,
        int userCount = 1)
    {
        var jsonEntityType = NetCordAsm.GetType("NetCord.JsonModels.JsonEntity")!;
        var jsonGuildType = NetCordAsm.GetType("NetCord.JsonModels.JsonGuild")!;
        var restGuildType = NetCordAsm.GetType("NetCord.Rest.RestGuild")!;

        var jsonGuild = CreateUninitialized("NetCord.JsonModels.JsonGuild");
        SetDeclaredField(jsonGuild, jsonEntityType, "<Id>k__BackingField", id);
        SetDeclaredField(jsonGuild, jsonGuildType, "<Name>k__BackingField", name);
        SetDeclaredField(jsonGuild, jsonGuildType, "<OwnerId>k__BackingField", ownerId);
        SetDeclaredField(jsonGuild, jsonGuildType, "<UserCount>k__BackingField", userCount);
        SetDeclaredField(jsonGuild, jsonGuildType, "<JoinedAt>k__BackingField", DateTimeOffset.UtcNow.AddDays(-1));

        var guild = (Guild)RuntimeHelpers.GetUninitializedObject(typeof(Guild));
        SetDeclaredField(guild, restGuildType, "_jsonModel", jsonGuild);

        return guild;
    }

    public static SlashCommandInteraction CreateSlashCommandInteraction(
        ulong userId,
        ulong? guildId = null,
        bool isBot = false,
        string username = "testuser")
    {
        var interactionType = NetCordAsm.GetType("NetCord.Interaction")!;
        var jsonEntityType = NetCordAsm.GetType("NetCord.JsonModels.JsonEntity")!;
        var jsonUserType = NetCordAsm.GetType("NetCord.JsonModels.JsonUser")!;

        var jsonUser = CreateUninitialized("NetCord.JsonModels.JsonUser");
        SetDeclaredField(jsonUser, jsonEntityType, "<Id>k__BackingField", userId);
        SetDeclaredField(jsonUser, jsonUserType, "<Username>k__BackingField", username);
        SetDeclaredField(jsonUser, jsonUserType, "<IsBot>k__BackingField", isBot);

        var user = (User)RuntimeHelpers.GetUninitializedObject(typeof(User));
        SetDeclaredField(user, typeof(User), "_jsonModel", jsonUser);

        var interaction = (SlashCommandInteraction)RuntimeHelpers.GetUninitializedObject(typeof(SlashCommandInteraction));
        SetDeclaredField(interaction, interactionType, "<User>k__BackingField", user);

        if (guildId.HasValue)
        {
            var guild = CreateGatewayGuild(guildId.Value, "TestGuild");
            SetDeclaredField(guild, typeof(Guild), "<Users>k__BackingField",
                new System.Collections.ObjectModel.ReadOnlyDictionary<ulong, GuildUser>(new Dictionary<ulong, GuildUser>()));
            SetDeclaredField(interaction, interactionType, "<Guild>k__BackingField", guild);
        }

        return interaction;
    }

    public static GatewayClient CreateUninitializedGatewayClient() =>
        (GatewayClient)RuntimeHelpers.GetUninitializedObject(typeof(GatewayClient));
}

internal static class HandlerFieldSetter
{
    public static void SetField(object obj, string fieldName, object? value)
    {
        var type = obj.GetType();
        while (type != null)
        {
            var field = type.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (field != null)
            {
                field.SetValue(obj, value);
                return;
            }
            type = type.BaseType;
        }
        throw new MissingFieldException(obj.GetType().FullName, fieldName);
    }
}
