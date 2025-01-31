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

public class ListAssignedCommand
{
    [RequireGuild]
    [Command("listassigned")]
    [Description("Lists tickets assigned to a user.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("user")] [Description("(Optional) User to list tickets for.")] DiscordUser user = null)
    {
        DiscordUser listUser = user == null ? command.User : user;

        if (!Database.Ticket.TryGetAssignedTickets(listUser.Id, out List<Database.Ticket> assignedTickets))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "User does not have any assigned tickets."
            });
            return;
        }

        List<string> listItems = new List<string>();
        foreach (Database.Ticket ticket in assignedTickets)
        {
            listItems.Add("**" + ticket.DiscordRelativeTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
        }

        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
        foreach (string message in Utilities.ParseListIntoMessages(listItems))
        {
            embeds.Add(new DiscordEmbedBuilder
            {
                Title = "Assigned tickets: ",
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