using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using MySqlConnector;

namespace SupportBoi.Commands;

public class RemoveStaffCommand
{
    [RequireGuild]
    [Command("removestaff")]
    [Description("Removes a staff member.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("user")] [Description("User to remove from staff.")] DiscordUser user)
    {
        if (!Database.StaffMember.IsStaff(user.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "User is already not registered as staff."
            }, true);
            return;
        }

        await command.DeferResponseAsync(true);

        if (Database.Ticket.TryGetAssignedTickets(user.Id, out List<Database.Ticket> assignedTickets))
        {
            foreach (Database.Ticket assignedTicket in assignedTickets)
            {
                Database.StaffMember.UnassignStaff(assignedTicket);
                try
                {
                    DiscordChannel ticketChannel = await SupportBoi.client.GetChannelAsync(assignedTicket.channelID);
                    await ticketChannel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Description = "Unassigned <@" + assignedTicket.assignedStaffID + "> from ticket."
                    });
                }
                catch (Exception e)
                {
                    Logger.Error("Error when trying to send message about unassigning staff member from ticket-" + assignedTicket.id.ToString("00000"), e);
                }

                await LogChannel.Success("<@" + assignedTicket.assignedStaffID + "> was unassigned from <#" + assignedTicket.channelID + "> by " + command.User.Mention + ".", assignedTicket.id);
            }
        }

        if (Database.StaffMember.RemoveStaff(user.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "User was removed from staff."
            }, true);

            await LogChannel.Success(user.Mention + " was removed from staff by " + command.User.Mention + ".");
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed to remove user from staff."
            }, true);
        }
    }
}