using CSharpFunctionalExtensions;
using Discord;
using Discord.Interactions;
using dotBento.Bot.AutoCompleteHandlers;
using dotBento.Bot.Services;
using dotBento.EntityFramework.Entities;
using dotBento.Infrastructure.Commands;
using dotBento.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace dotBento.Bot.Tests.AutoCompleteHandlers;

public sealed class SearchTagsWhenModifyAutoCompleteTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GenerateSuggestionsAsync_OnlyVerifiedManagersSeeOtherAuthorsTags(bool manager, bool lookupFailure)
    {
        var factory = new TestDbFactory();
        await using (var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Tags.AddRange(
                new Tag { GuildId = 100, UserId = 10, Command = "mine", Content = "my tag" },
                new Tag { GuildId = 100, UserId = 20, Command = "other", Content = "their tag" },
                new Tag { GuildId = 200, UserId = 10, Command = "different-guild", Content = "elsewhere" });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Id).Returns(100);
        var everyone = new Mock<IRole>();
        everyone.SetupGet(r => r.Permissions).Returns(new GuildPermissions(0));
        guild.SetupGet(g => g.EveryoneRole).Returns(everyone.Object);
        var role = new Mock<IRole>();
        role.SetupGet(r => r.Permissions).Returns(new GuildPermissions(manager ? (ulong)GuildPermission.ManageMessages : 0));
        guild.Setup(g => g.GetRole(7)).Returns(role.Object);
        var member = new Mock<IGuildUser>();
        member.SetupGet(m => m.GuildId).Returns(100);
        member.SetupGet(m => m.Id).Returns(10);
        member.SetupGet(m => m.RoleIds).Returns([7UL]);
        var actor = new Mock<IUser>();
        actor.SetupGet(u => u.Id).Returns(10);
        var resolver = new Mock<IDiscordGuildMemberResolver>();
        resolver.Setup(r => r.ResolveAsync(guild.Object, 10, null)).ReturnsAsync(lookupFailure
            ? Result.Failure<Maybe<IGuildUser>>("offline") : Result.Success(member.Object.AsMaybe()));
        var context = new Mock<IInteractionContext>();
        context.SetupGet(c => c.Guild).Returns(guild.Object);
        context.SetupGet(c => c.User).Returns(actor.Object);
        var handler = new SearchTagsWhenModifyAutoComplete(new TagCommands(new TagService(cache, factory)), resolver.Object);

        var result = await handler.GenerateSuggestionsAsync(context.Object, Mock.Of<IAutocompleteInteraction>(),
            Mock.Of<IParameterInfo>(), Mock.Of<IServiceProvider>());

        Assert.True(result.IsSuccess);
        Assert.Equal(manager && !lookupFailure ? ["mine", "other"] : new[] { "mine" }, result.Suggestions.Select(s => s.Name));
        Assert.DoesNotContain(result.Suggestions, s => s.Name == "different-guild");
    }
}
