using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;

namespace SupportBoi.Commands;

public class SetMessageCommand
{
    private readonly struct CommandInfo(string identifier, string message)
    {
        public string identifier { get; } = identifier;
        public string message { get; } = message;
    }

    private static Dictionary<ulong, CommandInfo> waitingCommands = new();

    [RequireGuild]
    [Command("setmessage")]
    [Description("Adds or updates message for the 'say' command.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("identifier")] [Description("The identifier word used in the /say command.")] string identifier,
        [Parameter("message")] [Description("The message the /say command will return. Empty to delete message.")] string message = "")
    {


        if (Database.Message.TryGetMessage(identifier.ToLower(CultureInfo.InvariantCulture), out Database.Message oldMessage))
        {
            if (string.IsNullOrEmpty(message))
            {
                await command.RespondAsync(new DiscordInteractionResponseBuilder()
                    .AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Orange,
                        Description = "Are you sure you want to delete the `" + identifier + "` message?"
                    })
                    .AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger, "supportboi_confirmmessagedelete " + command.Interaction.Id, "Confirm"),
                                           new DiscordButtonComponent(DiscordButtonStyle.Secondary, "supportboi_cancelmessagedelete " + command.Interaction.Id, "Cancel")));
            }
            else
            {
                await command.RespondAsync(new DiscordInteractionResponseBuilder()
                    .AddEmbed(new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Cyan,
                        Title = "Replace the `" + identifier + "` message?"
                    }
                    .AddField("Old message:", oldMessage.message.Truncate(1024)).AddField("New message:", message.Truncate(1024)))
                    .AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Success, "supportboi_confirmmessageupdate " + command.Interaction.Id, "Confirm"),
                                           new DiscordButtonComponent(DiscordButtonStyle.Secondary, "supportboi_cancelmessageupdate " + command.Interaction.Id, "Cancel")));
            }

            waitingCommands.Add(command.Interaction.Id, new CommandInfo(identifier, message));
        }
        else
        {
            if (string.IsNullOrEmpty(message))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Cannot delete that message, it does not exist."
                }, true);
            }
            else
            {
                await AddNewMessage(command, identifier, message);
            }
        }
    }

    private static async Task AddNewMessage(SlashCommandContext command, string identifier, string message)
    {
        if(Database.Message.AddMessage(identifier, command.Member.Id, message))
        {
            SayCommand.IdentifierAutoCompleteProvider.InvalidateCache();
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
            return;
        }

        await LogChannel.Success(command.User.Mention + " added the `" + identifier + "` message for the /say command.\n\n**Content:**\n\n" + message);
    }

    public static async Task ConfirmMessageDeletion(DiscordInteraction interaction, ulong previousInteractionID)
    {
        if (!waitingCommands.Remove(previousInteractionID, out CommandInfo command))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "I don't remember which message you wanted to delete, has the bot been restarted since the command was used?"
            }));
            return;
        }

        if (!Database.Message.TryGetMessage(command.identifier.ToLower(CultureInfo.InvariantCulture), out Database.Message _))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "There is no message with that identifier."
            }));
            return;
        }

        if (Database.Message.RemoveMessage(command.identifier))
        {
            SayCommand.IdentifierAutoCompleteProvider.InvalidateCache();
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Message removed."
            }));
        }
        else
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed removing the message from the database."
            }));
            return;
        }

        await LogChannel.Success("`" + command.identifier + "` was removed from the /say command by " + interaction.User.Mention + ".");
    }

    public static async Task CancelMessageDeletion(DiscordInteraction interaction, ulong previousInteractionID)
    {
        waitingCommands.Remove(previousInteractionID);
        if (interaction.Message != null)
        {
            await interaction.Message.DeleteAsync();
        }
    }

    public static async Task ConfirmMessageUpdate(DiscordInteraction interaction, ulong previousInteractionID)
    {
        if (!waitingCommands.Remove(previousInteractionID, out CommandInfo command))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "I don't remember which message you wanted to update, has the bot been restarted since the command was used?"
            }));
            return;
        }

        if (!Database.Message.TryGetMessage(command.identifier.ToLower(CultureInfo.InvariantCulture), out Database.Message _))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "There is no message with that identifier."
            }));
            return;
        }

        if (Database.Message.UpdateMessage(command.identifier, interaction.User.Id, command.message))
        {
            SayCommand.IdentifierAutoCompleteProvider.InvalidateCache();
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Message updated."
            }));
        }
        else
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed updating the message in the database."
            }));
            return;
        }

        await LogChannel.Success("`" + command.identifier + "` was updated for the /say command by " + interaction.User.Mention + ".");
    }

    public static async Task CancelMessageUpdate(DiscordInteraction interaction, ulong previousInteractionID)
    {
        waitingCommands.Remove(previousInteractionID);
        if (interaction.Message != null)
        {
            await interaction.Message.DeleteAsync();
        }
    }
}