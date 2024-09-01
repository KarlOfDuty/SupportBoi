using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class ToggleActiveCommand : ApplicationCommandModule
{
    [SlashRequireGuild]
    [SlashCommand("toggleactive", "Toggles active status for a staff member.")]
    public async Task OnExecute(InteractionContext command, [Option("User", "(Optional) Staff member to toggle activity for.")] DiscordUser user = null)
    {
        DiscordUser staffUser = user == null ? command.User : user;

        // Check if ticket exists in the database
        if (!Database.TryGetStaff(staffUser.Id, out Database.StaffMember staffMember))
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = user == null ? "You have not been registered as staff." : "The user is not registered as staff."
            }, true);
            return;
        }

        if (Database.SetStaffActive(staffUser.Id, !staffMember.active))
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = staffMember.active ? "Staff member is now set as inactive and will no longer be randomly assigned any support tickets." : "Staff member is now set as active and will be randomly assigned support tickets again."
            }, true);
        }
        else
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Unable to update active status in database."
            }, true);
        }
    }
}