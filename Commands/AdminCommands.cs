using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace SupportBoi.Commands;

[Command("admin")]
[Description("Administrative commands.")]
public class AdminCommands
{
    [RequireGuild]
    [Command("listinvalid")]
    [Description("List tickets which channels have been deleted. Use /admin unsetticket <id> to remove them.")]
    public async Task ListInvalid(SlashCommandContext command)
    {
        if (!Database.TryGetOpenTickets(out List<Database.Ticket> openTickets))
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
        List<string> listItems = new List<string>();
        foreach (Database.Ticket ticket in openTickets)
        {
            if (allChannels.All(channel => channel.Id != ticket.channelID))
            {
                listItems.Add("ID: **" + ticket.id.ToString("00000") + ":** <#" + ticket.channelID + ">\n");
            }
        }

        if (listItems.Count == 0)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "All tickets are valid!"
            }, true);
            return;
        }

        List<DiscordEmbedBuilder> embeds = new List<DiscordEmbedBuilder>();
        foreach (string message in Utilities.ParseListIntoMessages(listItems))
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

    [RequireGuild]
    [Command("setticket")]
    [Description("Turns a channel into a ticket. WARNING: Anyone will be able to delete the channel using /close.")]
    public async Task SetTicket(SlashCommandContext command,
        [Parameter("user")] [Description("(Optional) The owner of the ticket.")] DiscordUser user = null)
    {
        // Check if ticket exists in the database
        if (Database.IsOpenTicket(command.Channel.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is already a ticket."
            }, true);
            return;
        }

        DiscordUser ticketUser = (user == null ? command.User : user);

        long id = Database.NewTicket(ticketUser.Id, 0, command.Channel.Id);
        string ticketID = id.ToString("00000");
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Channel has been designated ticket " + ticketID + "."
        });

        // TODO: This throws an exception instead of returning null now

        // Log it if the log channel exists
        DiscordChannel logChannel = await command.Guild.GetChannelAsync(Config.logChannel);
        if (logChannel != null)
        {
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.Channel.Mention + " has been designated ticket " + ticketID + " by " + command.Member.Mention + "."
            });
        }
    }

    [RequireGuild]
    [Command("unsetticket")]
    [Description("Deletes a ticket from the ticket system without deleting the channel.")]
    public async Task UnsetTicket(SlashCommandContext command,
        [Parameter("ticket-id")] [Description("(Optional) Ticket to unset. Uses the channel you are in by default.")] long ticketID = 0)
    {
        Database.Ticket ticket;

        if (ticketID == 0)
        {
            // Check if ticket exists in the database
            if (!Database.TryGetOpenTicket(command.Channel.Id, out ticket))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "This channel is not a ticket!"
                }, true);
                return;
            }
        }
        else
        {
            // Check if ticket exists in the database
            if (!Database.TryGetOpenTicketByID((uint)ticketID, out ticket))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "There is no ticket with this ticket ID."
                }, true);
                return;
            }
        }


        if (Database.DeleteOpenTicket(ticket.id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Channel has been undesignated as a ticket."
            });

            // TODO: This throws an exception instead of returning null now

            // Log it if the log channel exists
            DiscordChannel logChannel = await command.Guild.GetChannelAsync(Config.logChannel);
            if (logChannel != null)
            {
                await logChannel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = command.Channel.Mention + " has been undesignated as a ticket by " + command.Member.Mention + "."
                });
            }
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed removing ticket from database."
            }, true);
        }
    }

    [Command("reload")]
    [Description("Reloads the bot config.")]
    public async Task Reload(SlashCommandContext command)
    {
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Reloading bot application..."
        });
        Logger.Log("Reloading bot...");
        SupportBoi.Reload();
    }
}