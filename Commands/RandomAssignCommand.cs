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
        DiscordMember staffMember = await GetRandomVerifiedStaffMember(command.Channel, ticket.creatorID, ticket.assignedStaffID, role);
        if (staffMember == null)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Could not find an applicable staff member with access to this channel."
            }, true);
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

        await LogChannel.Success(staffMember.Mention + " was randomly assigned to " + command.Channel.Mention + " by " + command.User.Mention + ".", ticket.id);
    }

    internal static async Task<DiscordMember> GetRandomVerifiedStaffMember(DiscordChannel channel, ulong creatorID, ulong currentStaffID, DiscordRole targetRole)
    {
        List<Database.StaffMember> staffMembers;
        ulong[] ignoredUserIDs = [creatorID, currentStaffID];

        if (targetRole == null)
        {
            // No role was specified, any active staff will be picked
            staffMembers = Database.GetActiveStaff(ignoredUserIDs);
        }
        else
        {
            // Check if role rassign should override staff's active status
            staffMembers = Config.randomAssignRoleOverride
                ? Database.GetAllStaff(ignoredUserIDs)
                : Database.GetActiveStaff(ignoredUserIDs);
        }

        // Randomize the list before checking for roles in order to reduce number of API calls
        staffMembers.Shuffle();

        // Get the first staff member that has the role
        foreach (Database.StaffMember staffMember in staffMembers)
        {
            try
            {
                DiscordMember verifiedMember = await channel.Guild.GetMemberAsync(staffMember.userID);

                // If a role is set filter to only members with that role
                if (targetRole == null || verifiedMember.Roles.Any(role => role.Id == targetRole.Id))
                {
                    // Only assign staff members with access to this channel
                    if (verifiedMember.PermissionsIn(channel).HasFlag(DiscordPermissions.AccessChannels))
                    {
                        return verifiedMember;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error occured trying to find a staff member for random assignment. User ID: " + staffMember.userID, e);
            }
        }

        return null;
    }
}