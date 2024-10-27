using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace SupportBoi.Commands;

public class ListOpen
{
    [RequireGuild]
    [Command("listopen")]
    [Description("Lists all open tickets, oldest first.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        if (!Database.TryGetOpenTickets(out List<Database.Ticket> openTickets))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Could not fetch any open tickets."
            });
            return;
        }

        List<string> listItems = new List<string>();
        foreach (Database.Ticket ticket in openTickets)
        {
            listItems.Add("**" + ticket.DiscordRelativeTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
        }

        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
        foreach (string message in Utilities.ParseListIntoMessages(listItems))
        {
            embeds.Add(new DiscordEmbedBuilder
            {
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

        List<Page> listPages = new List<Page>();
        foreach (DiscordEmbedBuilder embed in embeds)
        {
            listPages.Add(new Page("", embed));
        }

        await command.Interaction.SendPaginatedResponseAsync(true, command.User, listPages);
    }
}