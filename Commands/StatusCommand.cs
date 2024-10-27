using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace SupportBoi.Commands;

public class StatusCommand
{
    [RequireGuild]
    [Command("status")]
    [Description("Shows bot status and information.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        long openTickets = Database.GetNumberOfTickets();
        long closedTickets = Database.GetNumberOfClosedTickets();

        DiscordEmbed botInfo = new DiscordEmbedBuilder()
            .WithAuthor("KarlofDuty/SupportBoi @ GitHub", "https://github.com/KarlofDuty/SupportBoi", "https://karlofduty.com/img/tardisIcon.jpg")
            .WithTitle("Bot information")
            .WithColor(DiscordColor.Cyan)
            .AddField("Version:", SupportBoi.GetVersion(), true)
            .AddField("Open tickets:", openTickets + "", true)
            .AddField("Closed tickets:", closedTickets + " ", true)
            .AddField("Report bugs:", "[Github Issues](https://github.com/KarlofDuty/SupportBoi/issues)", true)
            .AddField("Commands:", "[Github Repository](https://github.com/KarlOfDuty/SupportBoi)", true)
            .AddField("Donate:", "[Github Sponsors](https://github.com/sponsors/KarlOfDuty)", true);
        await command.RespondAsync(botInfo);
    }
}