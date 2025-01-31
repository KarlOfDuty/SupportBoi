using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class UnassignCommand
{
    [RequireGuild]
    [Command("unassign")]
    [Description("Unassigns a staff member from a ticket.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        // Check if ticket exists in the database
        if (!Database.Ticket.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            }, true);
            return;
        }

        if (!Database.StaffMember.UnassignStaff(ticket))
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
            Description = "Unassigned <@" + ticket.assignedStaffID + "> from ticket."
        });

        await LogChannel.Success("<@" + ticket.assignedStaffID + "> was unassigned from <#" + ticket.channelID + "> by " + command.User.Mention + ".", ticket.id);
    }
}