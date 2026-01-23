using System.Text;

namespace dotBento.Infrastructure.Commands.Profile;

/// <summary>
/// Generates HTML for profile rendering
/// </summary>
public static class ProfileHtmlGenerator
{
    public static string Generate(ProfileViewModel viewModel, string css)
    {
        var profile = viewModel.Profile;
        var hasCustomBackground = profile.BackgroundUrl != null;

        var wrapperClass = hasCustomBackground ? "custom-bg" : "";
        var sidebarClass = hasCustomBackground ? "blur" : "";
        var overlayClass = hasCustomBackground ? "overlay" : "";

        var bodyHtml = GenerateBody(viewModel, wrapperClass, sidebarClass, overlayClass);

        var html = new StringBuilder();
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("    <link href='https://fonts.googleapis.com/css2?family=Urbanist:wght@400;700&display=swap' rel='stylesheet'>");
        html.AppendLine("    <meta charset='UTF-8'>");
        html.AppendLine("</head>");
        html.AppendLine("<style>");
        html.AppendLine(css);
        html.AppendLine("</style>");
        html.AppendLine("<body>");
        html.AppendLine(bodyHtml);
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    private static string GenerateBody(ProfileViewModel viewModel, string wrapperClass, string sidebarClass, string overlayClass)
    {
        var bentoSection = viewModel.HasBentoData
            ? $"<li class='sidebar-itemBento'><span class='sidebar-valueBento'>{viewModel.BentoCount} 🍱</span><br>Rank {viewModel.BentoRank}/{viewModel.TotalBentoUsers} 🍱 Users</li>"
            : "";

        var lastFmBoard = viewModel.LastFmBoardHtml ?? "";
        var xpBoard = viewModel.XpBoardHtml ?? "";

        var html = new StringBuilder();
        html.AppendLine($"<div class='wrapper {wrapperClass}'>");
        html.AppendLine($"    <div class='inner-wrapper {overlayClass}'>");
        html.AppendLine();
        html.AppendLine("        <div class='center-area'>");
        html.AppendLine("            <div class='description'>");
        html.AppendLine($"                <p class='description-text'>{viewModel.Description}</p>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");
        html.AppendLine();
        html.AppendLine($"        <div class='sidebar {sidebarClass}'>");
        html.AppendLine();
        html.AppendLine("            <div class='avatar-container'>");
        html.AppendLine($"                <img class='avatar' src='{viewModel.AvatarUrl}'>");
        html.AppendLine("            </div>");
        html.AppendLine();
        html.AppendLine("            <div class='name-container'>");
        html.AppendLine("                <svg width='200' height='50'>");
        html.AppendLine($"                    <text class='username' x='50%' y='30%' dominant-baseline='middle' text-anchor='middle'>");
        html.AppendLine($"                        {viewModel.Username}");
        html.AppendLine("                    </text>");
        html.AppendLine($"                    <text class='discriminator' x='50%' y='75%' dominant-baseline='middle' text-anchor='middle'>");
        html.AppendLine($"                        {viewModel.Discriminator}");
        html.AppendLine("                    </text>");
        html.AppendLine("                </svg>");
        html.AppendLine("            </div>");
        html.AppendLine();
        html.AppendLine("            <ul class='sidebar-list'>");
        html.AppendLine("                <li class='sidebar-itemServer'>");
        html.AppendLine($"                    <span class='sidebar-valueServer'>Rank {viewModel.ServerRank}</span><br>");
        html.AppendLine($"                    Of {viewModel.GuildUserCount} Users");
        html.AppendLine("                </li>");
        html.AppendLine("                <li class='sidebar-itemGlobal'>");
        html.AppendLine($"                    <span class='sidebar-valueGlobal'>Rank {viewModel.GlobalRank}</span><br>");
        html.AppendLine($"                    Of {viewModel.TotalUserCount:F1}k Users");
        html.AppendLine("                </li>");
        html.AppendLine($"                {bentoSection}");
        html.AppendLine("                <li class='sidebar-itemTimezone'>");
        html.AppendLine($"                    <span class='sidebar-valueEmote'>{string.Join("", viewModel.Emotes)}</span><br>");
        html.AppendLine($"                    {viewModel.TimezoneDisplay} {viewModel.BirthdayDisplay}");
        html.AppendLine("                </li>");
        html.AppendLine("            </ul>");
        html.AppendLine();
        html.AppendLine("        </div>");
        html.AppendLine("        <div class='footer'>");
        html.AppendLine($"            {lastFmBoard}");
        html.AppendLine($"            {xpBoard}");
        html.AppendLine("        </div>");
        html.AppendLine("    </div>");
        html.AppendLine("</div>");

        return html.ToString();
    }
}
