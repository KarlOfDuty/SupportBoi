using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

[SlashCommandGroup("admin", "Administrative commands.")]
public class AdminCommands : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("listinvalid", "List tickets which channels have been deleted. Use /admin unsetticket <id> to remove them.")]
	public async Task ListInvalid(InteractionContext command)
	{
		if (!Database.TryGetOpenTickets(out List<Database.Ticket> openTickets))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Could not get any open tickets from database."
			}, true);
		}

		// Get all channels in all guilds the bot is part of
		List<DiscordChannel> allChannels = new List<DiscordChannel>();
		foreach (KeyValuePair<ulong,DiscordGuild> guild in SupportBoi.discordClient.Guilds)
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
			await command.CreateResponseAsync(new DiscordEmbedBuilder
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
		
	[SlashRequireGuild]
	[SlashCommand("setticket", "Turns a channel into a ticket WARNING: Anyone will be able to delete the channel using /close.")]
	public async Task SetTicket(InteractionContext command, [Option("User", "(Optional) The owner of the ticket.")] DiscordUser user = null)
	{
		// Check if ticket exists in the database
		if (Database.IsOpenTicket(command.Channel.Id))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "This channel is already a ticket."
			}, true);
			return;
		}

		DiscordUser ticketUser = (user == null ? command.User : user);

		long id = Database.NewTicket(ticketUser.Id, 0, command.Channel.Id);
		string ticketID = id.ToString("00000");
		await command.CreateResponseAsync(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "Channel has been designated ticket " + ticketID + "."
		});

		// Log it if the log channel exists
		DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
		if (logChannel != null)
		{
			await logChannel.SendMessageAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = command.Channel.Mention + " has been designated ticket " + ticketID + " by " + command.Member.Mention + "."
			});
		}
	}
		
	[SlashRequireGuild]
	[SlashCommand("unsetticket", "Deletes a ticket from the ticket system without deleting the channel.")]
	public async Task UnsetTicket(InteractionContext command, [Option("TicketID", "(Optional) Ticket to unset. Uses the channel you are in by default.")] long ticketID = 0)
	{
		Database.Ticket ticket;

		if (ticketID == 0)
		{
			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(command.Channel.Id, out ticket))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
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
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "There is no ticket with this ticket ID."
				}, true);
				return;
			}
		}


		if (Database.DeleteOpenTicket(ticket.id))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Channel has been undesignated as a ticket."
			});
    
			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
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
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error: Failed removing ticket from database."
			}, true);
		}
	}
		
	[SlashCommand("reload", "Reloads the bot config.")]
	public async Task Reload(InteractionContext command)
	{
		await command.CreateResponseAsync(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "Reloading bot application..."
		});
		Logger.Log("Reloading bot...");
		SupportBoi.Reload();
	}
}