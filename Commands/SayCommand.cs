using System;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using System.Threading;

namespace SupportBoi.Commands;

public class SayCommand
{
    public class IdentifierAutoCompleteProvider : IAutoCompleteProvider
    {
        private static List<string> ids = [];
        private static DateTime lastRefresh = DateTime.MinValue;
        private static readonly Lock cacheLock = new();
        private static readonly int cacheMinutes = 15;

        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            if (string.IsNullOrWhiteSpace(context.UserInput))
            {
                return ValueTask.FromResult(Enumerable.Empty<DiscordAutoCompleteChoice>());
            }

            if (lastRefresh < DateTime.UtcNow.AddMinutes(-cacheMinutes))
            {
                lock (cacheLock)
                {
                    if (lastRefresh < DateTime.UtcNow.AddMinutes(-cacheMinutes))
                    {
                        ids = Database.Message.GetIDs();
                        lastRefresh = DateTime.UtcNow;
                    }
                }
            }

            SortedSet<string> exactMatchAtBeginning = [];
            SortedSet<string> exactMatchAnywhere = [];
            foreach(string id in ids)
            {
                if (id.StartsWith(context.UserInput, StringComparison.OrdinalIgnoreCase))
                {
                    exactMatchAtBeginning.Add(id);
                    continue;
                }

                if (id.Contains(context.UserInput, StringComparison.OrdinalIgnoreCase))
                {
                    exactMatchAnywhere.Add(id);
                }
            }

            IEnumerable<DiscordAutoCompleteChoice> choices = exactMatchAtBeginning
                .Concat(exactMatchAnywhere.Except(exactMatchAtBeginning))
                .Take(25)
                .Select(id => new DiscordAutoCompleteChoice(id, id));

            return ValueTask.FromResult(choices);
        }

        public static void InvalidateCache()
        {
            lock (cacheLock)
            {
                lastRefresh = DateTime.MinValue;
            }
        }
    }

    [RequireGuild]
    [Command("say")]
    [Description("Prints a message with information from staff. Use without identifier to list all identifiers.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("Identifier")]
        [Description("(Optional) The identifier word to summon a message.")]
        [SlashAutoCompleteProvider<IdentifierAutoCompleteProvider>]
        string identifier = null)
    {
        // Print list of all messages if no identifier is provided
        if (identifier == null)
        {
            await SendMessageList(command);
            return;
        }

        if (!Database.Message.TryGetMessage(identifier.ToLower(CultureInfo.InvariantCulture),
                                    out Database.Message message))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "There is no message with that identifier."
            }, true);
            return;
        }

        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Cyan,
            Description = message.message.Replace("\\n", "\n")
        });

        await LogChannel.Success(command.User.Mention + " posted the `" + message.identifier + "` message in " + command.Channel.Mention + ".");
    }

    private static async Task SendMessageList(SlashCommandContext command)
    {
        List<Database.Message> messages = Database.Message.GetAllMessages();
        if (messages.Count == 0)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
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