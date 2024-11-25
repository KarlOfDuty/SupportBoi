using System.ComponentModel;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class RemoveMessageCommand
{
    [RequireGuild]
    [Command("removemessage")]
    [Description("Removes a message from the 'say' command.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("identifier")] [Description("The identifier word used in the /say command.")] string identifier)
    {
        if (!Database.TryGetMessage(identifier.ToLower(), out Database.Message _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "There is no message with that identifier."
            }, true);
            return;
        }

        if (Database.RemoveMessage(identifier))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Message removed."
            }, true);
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed removing the message from the database."
            }, true);
        }

        try
        {
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "`" + identifier + "` was removed from the /say command by " + command.User.Mention + "."
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not send message in log channel.");
        }
    }
}