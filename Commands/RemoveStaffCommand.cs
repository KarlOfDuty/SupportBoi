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
        if (!Database.IsStaff(user.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "User is already not registered as staff."
            }, true);
            return;
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

        // TODO: This throws an exception instead of returning null now
        // Log it if the log channel exists
        DiscordChannel logChannel = await command.Guild.GetChannelAsync(Config.logChannel);
        if (logChannel != null)
        {
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "User was removed from staff.\n"
            });
        }
    }
}