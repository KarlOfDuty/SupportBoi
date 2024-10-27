using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace SupportBoi.Commands;

public class SummaryCommand
{
    [RequireGuild]
    [Command("summary")]
    [Description("Lists tickets assigned to a user.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            }, true);
            return;
        }

        DiscordEmbed channelInfo = new DiscordEmbedBuilder()
            .WithTitle("Channel information")
            .WithColor(DiscordColor.Cyan)
            .AddField("Ticket number:", ticket.id.ToString("00000"), true)
            .AddField("Ticket creator:", $"<@{ticket.creatorID}>", true)
            .AddField("Assigned staff:", ticket.assignedStaffID == 0 ? "Unassigned." : $"<@{ticket.assignedStaffID}>", true)
            .AddField("Creation time:", ticket.DiscordRelativeTime(), true)
            .AddField("Summary:", string.IsNullOrEmpty(ticket.summary) ? "No summary." : ticket.summary.Replace("\\n", "\n"));
        await command.RespondAsync(channelInfo);
    }
}