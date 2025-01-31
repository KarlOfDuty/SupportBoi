using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace SupportBoi.Commands;

public class ListInvalidCommand
{
    [RequireGuild]
    [Command("listinvalid")]
    [Description("List tickets which channels have been deleted. Use /admin unsetticket <id> to remove them.")]
    public async Task ListInvalid(SlashCommandContext command)
    {
        if (!Database.Ticket.TryGetOpenTickets(out List<Database.Ticket> openTickets))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Could not get any open tickets from database."
            }, true);
        }

        // Get all channels in all guilds the bot is part of
        List<DiscordChannel> allChannels = new List<DiscordChannel>();
        foreach (KeyValuePair<ulong,DiscordGuild> guild in SupportBoi.client.Guilds)
        {
            try
            {
                allChannels.AddRange(await guild.Value.GetChannelsAsync());
            }
            catch (Exception) { /*ignored*/ }
        }

        // Check which tickets channels no longer exist
        List<string> invalidTickets = new List<string>();
        foreach (Database.Ticket ticket in openTickets)
        {
            DiscordChannel channel = allChannels.FirstOrDefault(c => c.Id == ticket.channelID);
            if (channel == null)
            {
                invalidTickets.Add("**`ticket-" + ticket.id.ToString("00000") + ":`** Channel no longer exists (<#" + ticket.channelID + ">)\n");
                continue;
            }

            try
            {
                await channel.Guild.GetMemberAsync(ticket.creatorID);
            }
            catch (NotFoundException)
            {
                invalidTickets.Add("**`ticket-" + ticket.id.ToString("00000") + ":`** Creator has left (<#" + ticket.channelID + ">)\n");
            }
        }

        if (invalidTickets.Count == 0)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "All tickets are valid!"
            }, true);
            return;
        }

        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
        foreach (string message in Utilities.ParseListIntoMessages(invalidTickets))
        {
            embeds.Add(new DiscordEmbedBuilder
            {
                Title = "Invalid tickets:",
                Color = DiscordColor.Red,
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