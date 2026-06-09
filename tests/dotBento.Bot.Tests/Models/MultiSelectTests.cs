using Discord;
using dotBento.Bot.Models.Discord.MultiSelect;
using Fergun.Interactive;

namespace dotBento.Bot.Tests.Models;

public sealed class MultiSelectTests
{
    [Fact]
    public void Builder_UsesSelectMenusInputType()
    {
        var builder = new MultiSelectBuilder<string>();

        Assert.Equal(InputType.SelectMenus, builder.InputType);
    }

    [Fact]
    public void GetOrAddComponents_GroupsOptionsByRow()
    {
        var selection = new MultiSelectBuilder<string>()
            .WithSelectionPage(new PageBuilder().WithDescription("Select one"))
            .AddOption(new MultiSelectOption("First", "first", 0, "First option", true))
            .AddOption(new MultiSelectOption("Second", "second", 0, "Second option"))
            .AddOption(new MultiSelectOption("Third", "third", 1, "Third option"))
            .Build();

        var components = selection.GetOrAddComponents(disableAll: false);

        Assert.Collection(
            components.ActionRows,
            row =>
            {
                var menu = Assert.Single(row.Components.OfType<SelectMenuBuilder>());
                Assert.Equal("selectmenu0", menu.CustomId);
                Assert.False(menu.IsDisabled);
                Assert.Collection(
                    menu.Options,
                    option =>
                    {
                        Assert.Equal("First", option.Label);
                        Assert.Equal("first", option.Value);
                        Assert.Equal("First option", option.Description);
                        Assert.True(option.IsDefault);
                    },
                    option =>
                    {
                        Assert.Equal("Second", option.Label);
                        Assert.Equal("second", option.Value);
                        Assert.Equal("Second option", option.Description);
                        Assert.False(option.IsDefault);
                    });
            },
            row =>
            {
                var menu = Assert.Single(row.Components.OfType<SelectMenuBuilder>());
                Assert.Equal("selectmenu1", menu.CustomId);
                Assert.Single(menu.Options);
            });
    }

    [Fact]
    public void GetOrAddComponents_DisablesSelectMenusWhenRequested()
    {
        var selection = new MultiSelectBuilder<string>()
            .WithSelectionPage(new PageBuilder().WithDescription("Select one"))
            .AddOption(new MultiSelectOption("First", "first", 0, "First option"))
            .Build();

        var components = selection.GetOrAddComponents(disableAll: true);
        var menu = Assert.Single(components.ActionRows.Single().Components.OfType<SelectMenuBuilder>());

        Assert.True(menu.IsDisabled);
    }
}
