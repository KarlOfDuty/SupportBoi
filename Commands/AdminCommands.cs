using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using SupportBoi.Interviews;

namespace SupportBoi.Commands;

[Command("admin")]
[Description("Administrative commands.")]
public class AdminCommands
{
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

        DiscordUser ticketUser = user == null ? command.User : user;

        long id = Database.NewTicket(ticketUser.Id, 0, command.Channel.Id);
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Channel has been designated ticket " + id.ToString("00000") + "."
        });

        try
        {
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.Channel.Mention + " has been designated ticket " + id.ToString("00000") + " by " + command.Member?.Mention + ".",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Ticket: " + id.ToString("00000")
                }
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not find the log channel.");
        }
    }

    [RequireGuild]
    [Command("unsetticket")]
    [Description("Deletes a ticket from the ticket system without deleting the channel.")]
    public async Task UnsetTicket(SlashCommandContext command,
        [Parameter("ticket-id")] [Description("(Optional) Ticket to unset. Uses the channel you are in by default. Use ticket ID, not channel ID!")] long ticketID = 0)
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

        Database.TryDeleteInterview(ticket.channelID);

        if (Database.DeleteOpenTicket(ticket.id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Channel has been undesignated as a ticket."
            });

            try
            {
                // Log it if the log channel exists
                DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
                await logChannel.SendMessageAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = command.Channel.Mention + " has been undesignated as a ticket by " + command.User.Mention + ".",
                    Footer = new DiscordEmbedBuilder.EmbedFooter
                    {
                        Text = "Ticket: " + ticket.id.ToString("00000")
                    }
                });
            }
            catch (NotFoundException)
            {
                Logger.Error("Could not find the log channel.");
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

    [RequireGuild]
    [Command("reload")]
    [Description("Reloads the bot config.")]
    public async Task Reload(SlashCommandContext command)
    {
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Reloading bot application..."
        });

        try
        {
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.Channel.Mention + " reloaded the bot.",
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not find the log channel.");
        }

        Logger.Log("Reloading bot...");
        await SupportBoi.Reload();
    }
}