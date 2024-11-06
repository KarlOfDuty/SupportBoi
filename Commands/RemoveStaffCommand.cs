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
            Logger.Error("Could not find the log channel.");
        }
    }
}