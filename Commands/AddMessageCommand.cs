using System.ComponentModel;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class AddMessageCommand
{
    [RequireGuild]
    [Command("addmessage")]
    [Description("Adds a new message for the 'say' command.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("identifier")] [Description("The identifier word used in the /say command.")] string identifier,
        [Parameter("message")] [Description("The message the /say command will return.")] string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "No message specified."
            }, true);
            return;
        }

        if (Database.TryGetMessage(identifier.ToLower(), out Database.Message _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "There is already a message with that identifier."
            }, true);
            return;
        }

        if(Database.AddMessage(identifier, command.Member.Id, message))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Message added."
            }, true);
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed adding the message to the database."
            }, true);
        }

        try
        {
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.User.Mention + " added or updated `" + identifier + "` in the /say command.\n\nContent:\n\n" + message,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Identifier: " + identifier
                }
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not find the log channel.");
        }
    }
}