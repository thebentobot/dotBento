using Discord.Interactions;

namespace dotBento.Bot.Utilities;

public static class SlashCommandIdUtilities
{
    public const string ServerSettingsCommandId = "server:settings";

    public static string ResolveCommandId(ICommandInfo command)
    {
        var parts = new List<string>();
        var module = command.Module;

        while (module is not null)
        {
            if (module.SlashGroupName is not null)
                parts.Insert(0, module.SlashGroupName);
            module = module.Parent;
        }

        parts.Add(command.Name);
        return string.Join(":", parts);
    }

    public static string[] GetManageableCommandIds(InteractionService interactionService) =>
        interactionService.SlashCommands
            .Select(ResolveCommandId)
            .Where(id => !IsProtectedCommand(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static bool IsProtectedCommand(string commandId) =>
        commandId.Equals(ServerSettingsCommandId, StringComparison.OrdinalIgnoreCase);
}
