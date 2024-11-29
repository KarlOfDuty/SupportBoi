using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands;

public class RandomAssignCommand
{
    [RequireGuild]
    [Command("rassign")]
    [Description("Randomly assigns a staff member to a ticket.")]
    public async Task OnExecute(SlashCommandContext command, [Parameter("role")] [Description("(Optional) Limit the random assignment to a specific role.")] DiscordRole role = null)
    {
        // Check if ticket exists in the database
        if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: This channel is not a ticket."
            }, true);
            return;
        }

        // Get a random staff member who is verified to have the correct role if applicable
        DiscordMember staffMember = await GetRandomVerifiedStaffMember(command, role, ticket);
        if (staffMember == null)
        {
            return;
        }

        // Attempt to assign the staff member to the ticket
        if (!Database.AssignStaff(ticket, staffMember.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed to assign " + staffMember.Mention + " to ticket."
            }, true);
            return;
        }

        // Respond that the command was successful
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Randomly assigned " + staffMember.Mention + " to ticket."
        });

        // Send a notification to the staff member if applicable
        if (Config.assignmentNotifications)
        {
            try
            {
                await staffMember.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "You have been randomly assigned to a support ticket: " + command.Channel.Mention
                });
            }
            catch (UnauthorizedException) { /* ignore */ }
        }

        try
        {
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = staffMember.Mention + " was randomly assigned to " + command.Channel.Mention + " by " + command.User.Mention + ".",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Ticket: " + ticket.id.ToString("00000")
                }
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not send message in log channel.");
        }
    }

    private static async Task<DiscordMember> GetRandomVerifiedStaffMember(SlashCommandContext command, DiscordRole targetRole, Database.Ticket ticket)
    {
        if (targetRole != null) // A role was provided
        {
            // Check if role rassign should override staff's active status
            List<Database.StaffMember> staffMembers = Config.randomAssignRoleOverride
                ? Database.GetAllStaff(ticket.assignedStaffID, ticket.creatorID)
                : Database.GetActiveStaff(ticket.assignedStaffID, ticket.creatorID);

            // Randomize the list before checking for roles in order to reduce number of API calls
            staffMembers.Shuffle();

            // Get the first staff member that has the role
            foreach (Database.StaffMember sm in staffMembers)
            {
                try
                {
                    DiscordMember verifiedMember = await command.Guild.GetMemberAsync(sm.userID);
                    if (verifiedMember?.Roles?.Any(role => role.Id == targetRole.Id) ?? false)
                    {
                        return verifiedMember;
                    }
                }
                catch (Exception e)
                {
                    command.Client.Logger.Log(LogLevel.Information, e, "Error occured trying to find a staff member in the rassign command.");
                }
            }
        }
        else // No role was specified, any active staff will be picked
        {
            Database.StaffMember staffEntry = Database.GetRandomActiveStaff(ticket.assignedStaffID, ticket.creatorID);
            if (staffEntry == null)
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Error: There are no other staff members to choose from."
                }, true);
                return null;
            }

            // Get the staff member from discord
            try
            {
                return await command.Guild.GetMemberAsync(staffEntry.userID);
            }
            catch (NotFoundException) { /* ignore */ }
        }

        // Send a more generic error if we get to this point and still haven't found the staff member
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Red,
            Description = "Error: Could not find an applicable staff member."
        }, true);
        return null;
    }
}