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

public class ListCommand
{
    [RequireGuild]
    [Command("list")]
    [Description("Lists tickets opened by a user.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("user")] [Description("(Optional) The user whose tickets to get, yourself by default.")] DiscordUser user = null)
    {
        DiscordUser listUser = user == null ? command.User : user;

        List<DiscordEmbedBuilder> openEmbeds = new List<DiscordEmbedBuilder>();
        if (Database.TryGetOpenTickets(listUser.Id, out List<Database.Ticket> openTickets))
        {
            List<string> listItems = new List<string>();
            foreach (Database.Ticket ticket in openTickets)
            {
                listItems.Add("**" + ticket.DiscordRelativeTime() + ":** <#" + ticket.channelID + ">\n");
            }

            foreach (string message in Utilities.ParseListIntoMessages(listItems))
            {
                openEmbeds.Add(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = message
                });
            }

            // Add the titles
            for (int i = 0; i < openEmbeds.Count; i++)
            {
                openEmbeds[i].Title = $"Open tickets ({i+1}/{openEmbeds.Count})";
            }
        }

        List<DiscordEmbedBuilder> closedEmbeds = new List<DiscordEmbedBuilder>();
        if (Database.TryGetClosedTickets(listUser.Id, out List<Database.Ticket> closedTickets))
        {
            List<string> listItems = new List<string>();
            foreach (Database.Ticket ticket in closedTickets)
            {
                listItems.Add("**" + ticket.DiscordRelativeTime() + ":** Ticket " + ticket.id.ToString("00000") + "\n");
            }

            foreach (string message in Utilities.ParseListIntoMessages(listItems))
            {
                closedEmbeds.Add(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = message
                });
            }

            // Add the titles
            for (int i = 0; i < closedEmbeds.Count; i++)
            {
                closedEmbeds[i].Title = $"Closed tickets ({i+1}/{closedEmbeds.Count})";
            }
        }

        // Merge the embed lists and add the footers
        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
        embeds.AddRange(openEmbeds);
        embeds.AddRange(closedEmbeds);
        for (int i = 0; i < embeds.Count; i++)
        {
            embeds[i].Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"Page {i + 1} / {embeds.Count}"
            };
        }

        if (embeds.Count == 0)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Cyan,
                Description = "User does not have any open or closed tickets."
            });
            return;
        }

        List<Page> listPages = new List<Page>();
        foreach (DiscordEmbedBuilder embed in embeds)
        {
            listPages.Add(new Page("", embed));
        }

        await command.Interaction.SendPaginatedResponseAsync(true, command.User, listPages);
    }
}