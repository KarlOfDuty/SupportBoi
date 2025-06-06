﻿using System;
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

public class ListOpen
{
    [RequireGuild]
    [Command("listopen")]
    [Description("Lists all open tickets, oldest first.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        if (!Database.Ticket.TryGetOpenTickets(out List<Database.Ticket> openTickets))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Could not fetch any open tickets."
            }, true);
            return;
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

        List<string> listItems = new List<string>();
        foreach (Database.Ticket ticket in openTickets)
        {
            try
            {
                DiscordChannel channel = allChannels.FirstOrDefault(c => c.Id == ticket.channelID);
                if (channel != null)
                {
                    if (command.Member!.PermissionsIn(channel).HasPermission(DiscordPermission.ViewChannel))
                    {
                        listItems.Add("**" + ticket.DiscordRelativeTime() + ":** <#" + ticket.channelID + "> by <@" + ticket.creatorID + ">\n");
                    }
                }
            }
            catch (NotFoundException) { /*ignored*/ }
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