using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class ToggleActiveCommand
{
    [RequireGuild]
    [Command("toggleactive")]
    [Description("Toggles active status for a staff member.")]
    public async Task OnExecute(SlashCommandContext command, [Parameter("user")] [Description("(Optional) Staff member to toggle activity for.")] DiscordUser user = null)
    {
        DiscordUser targetUser = user == null ? command.User : user;

        // Check if ticket exists in the database
        if (!Database.StaffMember.TryGetStaff(targetUser.Id, out Database.StaffMember targetStaffMember))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = user == null ? "You have not been registered as staff." : "The user is not registered as staff."
            }, true);
            return;
        }

        if (Database.StaffMember.SetStaffActive(targetUser.Id, !targetStaffMember.active))
        {
            if (user != null && user.Id != command.User.Id)
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = targetUser.Mention + (targetStaffMember.active ? " is now set as inactive and will no longer be randomly assigned any support tickets."
                                                                          : " is now set as active and will be randomly assigned support tickets again.")
                }, true);
            }
            else
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = targetStaffMember.active ? "You are now set as inactive and will no longer be randomly assigned any support tickets."
                                                     : "You are now set as active and will be randomly assigned support tickets again."
                }, true);
            }
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Unable to update active status in database."
            }, true);
        }

        if (user != null && user.Id != command.User.Id)
        {
            await LogChannel.Success(command.User.Mention + " set " + targetUser.Mention + "'s status to " + (targetStaffMember.active ? "inactive" : "active"));
        }
        else
        {
            await LogChannel.Success(command.User.Mention + " set their own status to " + (targetStaffMember.active ? "inactive" : "active"));
        }
    }
}