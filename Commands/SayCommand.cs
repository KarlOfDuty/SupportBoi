using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class SayCommand : ApplicationCommandModule
{
    [SlashRequireGuild]
    [SlashCommand("say", "Prints a message with information from staff. Use without identifier to list all identifiers.")]
    public async Task OnExecute(InteractionContext command, [Option("Identifier", "(Optional) The identifier word to summon a message.")] string identifier = null)
    {
        // Print list of all messages if no identifier is provided
        if (identifier == null)
        {
            SendMessageList(command);
            return;
        }

        if (!Database.TryGetMessage(identifier.ToLower(), out Database.Message message))
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "There is no message with that identifier."
            }, true);
            return;
        }

        await command.CreateResponseAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Cyan,
            Description = message.message.Replace("\\n", "\n")
        });
    }

    private static async void SendMessageList(InteractionContext command)
    {
        List<Database.Message> messages = Database.GetAllMessages();
        if (messages.Count == 0)
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "There are no messages registered."
            }, true);
            return;
        }

        List<string> listItems = [];
        foreach (Database.Message message in messages)
        {
            listItems.Add("**" + message.identifier + "** Added by <@" + message.userID + ">\n");
        }

        List<DiscordEmbedBuilder> embeds = [];
        foreach (string message in Utilities.ParseListIntoMessages(listItems))
        {
            embeds.Add(new DiscordEmbedBuilder
            {
                Title = "Available messages:",
                Color = DiscordColor.Green,
                Description = message
            });
        }

        // Add the footers
        for (int i = 0; i < embeds.Count; i++)
        {
            embeds[i].Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"Page {i + 1} / {embeds.Count}"
            };
        }

        List<Page> listPages = [];
        foreach (DiscordEmbedBuilder embed in embeds)
        {
            listPages.Add(new Page("", embed));
        }

        await command.Interaction.SendPaginatedResponseAsync(true, command.User, listPages);
    }
}