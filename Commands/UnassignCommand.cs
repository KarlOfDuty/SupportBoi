using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace SupportBoi.Commands;

public class UnassignCommand
{
    [RequireGuild]
    [Command("unassign")]
    [Description("Unassigns a staff member from a ticket.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        // Check if ticket exists in the database
        if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            }, true);
            return;
        }

        if (!Database.UnassignStaff(ticket))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed to unassign staff member from ticket."
            }, true);
            return;
        }

        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Unassigned staff member from ticket."
        });

        // TODO: This throws an exception instead of returning null now
        // Log it if the log channel exists
        DiscordChannel logChannel = await command.Guild.GetChannelAsync(Config.logChannel);
        if (logChannel != null)
        {
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Staff member was unassigned from " + command.Channel.Mention + " by " + command.Member.Mention + "."
            });
        }
    }
}