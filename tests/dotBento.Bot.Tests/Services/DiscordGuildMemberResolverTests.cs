using System.Net;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Net;
using dotBento.Bot.Extensions;
using dotBento.Bot.Services;
using Moq;

namespace dotBento.Bot.Tests.Services;

public sealed class DiscordGuildMemberResolverTests
{
    private readonly DiscordGuildMemberResolver _resolver = new();

    private static Mock<IGuildUser> Member(ulong guildId = 1, ulong userId = 2)
    {
        var member = new Mock<IGuildUser>(MockBehavior.Strict);
        member.SetupGet(m => m.Id).Returns(userId);
        member.SetupGet(m => m.GuildId).Returns(guildId);
        return member;
    }

    private static Mock<IGuild> Guild()
    {
        var guild = new Mock<IGuild>(MockBehavior.Strict);
        guild.SetupGet(g => g.Id).Returns(1);
        return guild;
    }

    [Fact]
    public async Task ResolveAsync_InteractionMember_AvoidsCacheAndRest()
    {
        var guild = Guild();
        var supplied = Member();
        var result = await _resolver.ResolveAsync(guild.Object, 2, supplied.Object);
        Assert.True(result.IsSuccess);
        Assert.Same(supplied.Object, result.Value.Value);
        guild.Verify(g => g.GetUserAsync(It.IsAny<ulong>(), It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_CachedMember_AvoidsRest()
    {
        var guild = Guild();
        var member = Member();
        guild.Setup(g => g.GetUserAsync(2, CacheMode.CacheOnly, null)).ReturnsAsync(member.Object);
        var result = await _resolver.ResolveAsync(guild.Object, 2);
        Assert.Same(member.Object, result.Value.Value);
        guild.Verify(g => g.GetUserAsync(2, CacheMode.AllowDownload, It.IsAny<RequestOptions>()), Times.Never);
    }

    [Theory]
    [InlineData(99UL, 2UL)]
    [InlineData(1UL, 99UL)]
    public async Task ResolveAsync_UnrelatedInteractionMember_UsesTargetedRest(ulong suppliedGuild, ulong suppliedUser)
    {
        var guild = Guild();
        var member = Member();
        guild.Setup(g => g.GetUserAsync(2, CacheMode.CacheOnly, null)).ReturnsAsync((IGuildUser)null!);
        guild.Setup(g => g.GetUserAsync(2, CacheMode.AllowDownload, It.Is<RequestOptions>(o => o.Timeout == 10_000)))
            .ReturnsAsync(member.Object);
        var result = await _resolver.ResolveAsync(guild.Object, 2, Member(suppliedGuild, suppliedUser).Object);
        Assert.Same(member.Object, result.Value.Value);
        guild.Verify(g => g.GetUserAsync(2, CacheMode.AllowDownload, It.IsAny<RequestOptions>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_AfterCacheCleared_FetchesMemberWithoutRepopulatingCache()
    {
        var guild = Guild();
        var member = Member();
        guild.SetupSequence(g => g.GetUserAsync(2, CacheMode.CacheOnly, null))
            .ReturnsAsync(member.Object).ReturnsAsync((IGuildUser)null!).ReturnsAsync((IGuildUser)null!);
        guild.Setup(g => g.GetUserAsync(2, CacheMode.AllowDownload, It.IsAny<RequestOptions>())).ReturnsAsync(member.Object);

        for (var i = 0; i < 3; i++)
            Assert.Same(member.Object, (await _resolver.ResolveAsync(guild.Object, 2)).Value.Value);

        guild.Verify(g => g.GetUserAsync(2, CacheMode.AllowDownload, It.IsAny<RequestOptions>()), Times.Exactly(2));
        guild.Verify(g => g.GetUsersAsync(It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveAsync_AbsentMember_ReturnsSuccessfulNone(bool throwsNotFound)
    {
        var guild = Guild();
        guild.Setup(g => g.GetUserAsync(2, CacheMode.CacheOnly, null)).ReturnsAsync((IGuildUser)null!);
        var lookup = guild.Setup(g => g.GetUserAsync(2, CacheMode.AllowDownload, It.IsAny<RequestOptions>()));
        if (throwsNotFound) lookup.ThrowsAsync(new HttpException(HttpStatusCode.NotFound, null!));
        else lookup.ReturnsAsync((IGuildUser)null!);
        var result = await _resolver.ResolveAsync(guild.Object, 2);
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasNoValue);
    }

    [Fact]
    public async Task ResolveAsync_TransportFailure_ReturnsFailureAndDoesNotCacheIt()
    {
        var guild = Guild();
        var member = Member();
        guild.Setup(g => g.GetUserAsync(2, CacheMode.CacheOnly, null)).ReturnsAsync((IGuildUser)null!);
        guild.SetupSequence(g => g.GetUserAsync(2, CacheMode.AllowDownload, It.IsAny<RequestOptions>()))
            .ThrowsAsync(new HttpRequestException("offline")).ReturnsAsync(member.Object);
        var failed = await _resolver.ResolveAsync(guild.Object, 2);
        Assert.True(failed.IsFailure);
        Assert.Contains("try again", failed.Error);
        Assert.Same(member.Object, (await _resolver.ResolveAsync(guild.Object, 2)).Value.Value);
    }

    [Theory]
    [InlineData(GuildPermission.ManageGuild, true)]
    [InlineData(GuildPermission.Administrator, true)]
    [InlineData(GuildPermission.ManageMessages, false)]
    public async Task HasAsync_UncachedMember_ResolvesGuildRolesAndDeniesUnrelatedPermissions(GuildPermission granted, bool expected)
    {
        var guild = Guild();
        guild.SetupGet(g => g.OwnerId).Returns(99);
        var everyone = new Mock<IRole>();
        everyone.SetupGet(r => r.Permissions).Returns(new GuildPermissions(0));
        guild.SetupGet(g => g.EveryoneRole).Returns(everyone.Object);
        var role = new Mock<IRole>();
        role.SetupGet(r => r.Permissions).Returns(new GuildPermissions((ulong)granted));
        guild.Setup(g => g.GetRole(10)).Returns(role.Object);
        var member = Member();
        member.SetupGet(m => m.RoleIds).Returns([10UL]);
        guild.Setup(g => g.GetUserAsync(2, CacheMode.CacheOnly, null)).ReturnsAsync((IGuildUser)null!);
        guild.Setup(g => g.GetUserAsync(2, CacheMode.AllowDownload, It.IsAny<RequestOptions>())).ReturnsAsync(member.Object);
        var actor = new Mock<IUser>();
        actor.SetupGet(u => u.Id).Returns(2);
        Assert.Equal(expected, await GuildMemberPermissions.HasAsync(_resolver, guild.Object, actor.Object, GuildPermission.ManageGuild));
        member.VerifyGet(m => m.GuildPermissions, Times.Never); // A REST member may have no parent guild.
    }

    [Fact]
    public async Task HasAsync_Owner_IsGrantedWithoutRoleLookup()
    {
        var guild = Guild();
        guild.SetupGet(g => g.OwnerId).Returns(2);
        Assert.True(await GuildMemberPermissions.HasAsync(_resolver, guild.Object, Member().Object, GuildPermission.ManageGuild));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HasAsync_MissingMemberOrLookupFailure_Denies(bool failure)
    {
        var resolver = new Mock<IDiscordGuildMemberResolver>();
        resolver.Setup(r => r.ResolveAsync(It.IsAny<IGuild>(), 2, It.IsAny<IGuildUser>()))
            .ReturnsAsync(failure ? Result.Failure<Maybe<IGuildUser>>("offline") : Result.Success(Maybe<IGuildUser>.None));
        Assert.False(await GuildMemberPermissions.HasAsync(resolver.Object, Guild().Object, Member().Object, GuildPermission.ManageGuild));
    }

    [Fact]
    public void Appearance_MissingGuildMember_UsesGlobalDetails()
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.GlobalName).Returns("Global name");
        user.Setup(u => u.GetDisplayAvatarUrl(ImageFormat.Auto, 128)).Returns("https://example.com/global.png");
        Assert.Equal(("Global name", "https://example.com/global.png"), GuildMemberExtensions.Appearance(user.Object, Maybe<IGuildUser>.None));
    }

    [Fact]
    public void Appearance_RestGuildMember_UsesGuildDetails()
    {
        var member = Member();
        member.SetupGet(m => m.Nickname).Returns("Server name");
        member.Setup(m => m.GetDisplayAvatarUrl(ImageFormat.Auto, 128)).Returns("https://example.com/guild.png");
        Assert.Equal(("Server name", "https://example.com/guild.png"), GuildMemberExtensions.Appearance(Mock.Of<IUser>(), member.Object.AsMaybe()));
    }
}
