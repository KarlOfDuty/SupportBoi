using System.ComponentModel;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;

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
    }
}