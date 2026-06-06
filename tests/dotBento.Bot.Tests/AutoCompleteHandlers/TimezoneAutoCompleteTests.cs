using Discord;
using dotBento.Bot.AutoCompleteHandlers;
using Moq;
using System.Runtime.CompilerServices;

namespace dotBento.Bot.Tests.AutoCompleteHandlers;

public sealed class TimezoneAutoCompleteTests
{
    [Fact]
    public async Task GenerateSuggestionsAsync_ReturnsFirstTwentyFiveZonesForBlankQuery()
    {
        var handler = new TimezoneAutoComplete();
        var interaction = CreateInteraction("   ");

        var result = await handler.GenerateSuggestionsAsync(
            Mock.Of<IInteractionContext>(),
            interaction,
            parameter: null!,
            services: Mock.Of<IServiceProvider>());

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Suggestions.Count);
    }

    [Fact]
    public async Task GenerateSuggestionsAsync_PrioritizesMatchingTimezoneIds()
    {
        var handler = new TimezoneAutoComplete();
        var interaction = CreateInteraction("Europe/Copen");

        var result = await handler.GenerateSuggestionsAsync(
            Mock.Of<IInteractionContext>(),
            interaction,
            parameter: null!,
            services: Mock.Of<IServiceProvider>());

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Suggestions, suggestion => suggestion.Value?.ToString() == "Europe/Copenhagen");
    }

    private static IAutocompleteInteraction CreateInteraction(string? currentValue)
    {
        var data = new Mock<IAutocompleteInteractionData>();
        data.SetupGet(d => d.Current).Returns(CreateOption(currentValue ?? string.Empty));

        var interaction = new Mock<IAutocompleteInteraction>();
        interaction.SetupGet(i => i.Data).Returns(data.Object);
        return interaction.Object;
    }

    private static AutocompleteOption CreateOption(string value)
    {
        var option = (AutocompleteOption)RuntimeHelpers.GetUninitializedObject(typeof(AutocompleteOption));
        typeof(AutocompleteOption)
            .GetField("<Value>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(option, value);
        return option;
    }
}
