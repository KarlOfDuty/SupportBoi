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

public class ListUnassignedCommand
{
    [RequireGuild]
    [Command("listunassigned")]
    [Description("Lists unassigned tickets.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        if (!Database.TryGetAssignedTickets(0, out List<Database.Ticket> unassignedTickets))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "There are no unassigned tickets."
            });
            return;
        }

        List<string> listItems = new List<string>();
        foreach (Database.Ticket ticket in unassignedTickets)
        {
            listItems.Add("**" + ticket.DiscordRelativeTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
        }

        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
        foreach (string message in Utilities.ParseListIntoMessages(listItems))
        {
            embeds.Add(new DiscordEmbedBuilder
            {
                Title = "Unassigned tickets: ",
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