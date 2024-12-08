using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
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
        if (!Database.IsStaff(user.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "User is already not registered as staff."
            }, true);
            return;
        }

        await command.DeferResponseAsync(true);

        if (Database.TryGetAssignedTickets(user.Id, out List<Database.Ticket> assignedTickets))
        {
            foreach (Database.Ticket assignedTicket in assignedTickets)
            {
                Database.UnassignStaff(assignedTicket);
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

                try
                {
                    // Log it if the log channel exists
                    DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
                    await logChannel.SendMessageAsync(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Description = "Staff member was unassigned from " + command.Channel.Mention + " by " + command.User.Mention + "."
                    });
                }
                catch (Exception e)
                {
                    Logger.Error("Could not send message in log channel.", e);
                }
            }
        }

        await using MySqlConnection c = Database.GetConnection();
        c.Open();
        MySqlCommand deletion = new MySqlCommand(@"DELETE FROM staff WHERE user_id=@user_id", c);
        deletion.Parameters.AddWithValue("@user_id", user.Id);
        await deletion.PrepareAsync();
        deletion.ExecuteNonQuery();

        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "User was removed from staff."
        }, true);

        try
        {
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = user.Mention + " was removed from staff by " + command.User.Mention + "."
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not send message in log channel.");
        }
    }
}