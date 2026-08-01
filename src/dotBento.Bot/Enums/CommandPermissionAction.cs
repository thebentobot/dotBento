namespace dotBento.Bot.Enums;

public enum CommandPermissionAction
{
    Disable,
    Enable,
    AddAdminOnly,
    RemoveAdminOnly
}

public static class CommandPermissionActionExtensions
{
    public static string ToToken(this CommandPermissionAction action) =>
        action switch
        {
            CommandPermissionAction.Disable => "disable",
            CommandPermissionAction.Enable => "enable",
            CommandPermissionAction.AddAdminOnly => "admin-add",
            CommandPermissionAction.RemoveAdminOnly => "admin-remove",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

    public static string ToLabel(this CommandPermissionAction action) =>
        action switch
        {
            CommandPermissionAction.Disable => "Disable",
            CommandPermissionAction.Enable => "Enable",
            CommandPermissionAction.AddAdminOnly => "Make Admin-Only",
            CommandPermissionAction.RemoveAdminOnly => "Remove Admin-Only",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };

    public static bool TryParseCommandPermissionAction(this string token, out CommandPermissionAction action)
    {
        action = token switch
        {
            "disable" => CommandPermissionAction.Disable,
            "enable" => CommandPermissionAction.Enable,
            "admin-add" => CommandPermissionAction.AddAdminOnly,
            "admin-remove" => CommandPermissionAction.RemoveAdminOnly,
            _ => default
        };

        return token is "disable" or "enable" or "admin-add" or "admin-remove";
    }
}
