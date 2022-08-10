using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;
public class NewCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[Config.ConfigPermissionCheckAttribute("new")]
	[SlashCommand("new", "Opens a new ticket.")]
	public async Task OnExecute(InteractionContext command)
	{
		await command.DeferAsync(true);
		(bool success, string message) = await OpenNewTicket(command.User.Id, command.Channel.Id, Config.ticketCategory);
		
		if (success)
		{
			await command.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = message
			}).AsEphemeral());
		}
		else
		{
			await command.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = message
			}).AsEphemeral());
		}
	}

	public static async Task<(bool, string)> OpenNewTicket(ulong userID, ulong commandChannelID, ulong categoryID)
	{
		// Check if user is blacklisted
		if (Database.IsBlacklisted(userID))
		{
			return (false, "You are banned from opening tickets.");
		}

		if (Database.IsOpenTicket(commandChannelID))
		{
			return (false, "You cannot use this command in a ticket channel.");
		}

		DiscordChannel category = null;
		try
		{
			category = await SupportBoi.discordClient.GetChannelAsync(categoryID);
		}
		catch (Exception) { /*ignored*/ }

		if (category == null)
		{
			return (false, "Error: Could not find the category to place the ticket in.");
		}

		DiscordMember member = null;
		try
		{
			member = await category.Guild.GetMemberAsync(userID);
		}
		catch (Exception) { /*ignored*/ }

		if (member == null)
		{
			return (false, "Error: Could not find you on the Discord server.");
		}
		
		DiscordChannel ticketChannel;

		try
		{
			ticketChannel = await category.Guild.CreateChannelAsync("ticket", ChannelType.Text, category);
		}
		catch (Exception)
		{
			return (false, "Error occured while creating ticket, " + member.Mention + 
						   "!\nIs the channel limit reached in the server or ticket category?");
		}

		if (ticketChannel == null)
		{
			return (false, "Error occured while creating ticket, " + member.Mention +
						   "!\nIs the channel limit reached in the server or ticket category?");
		}

		ulong staffID = 0;
		if (Config.randomAssignment)
		{
			staffID = Database.GetRandomActiveStaff(0)?.userID ?? 0;
		}

		long id = Database.NewTicket(member.Id, staffID, ticketChannel.Id);
		string ticketID = id.ToString("00000");
		await ticketChannel.ModifyAsync(modifiedAttributes => modifiedAttributes.Name = "ticket-" + ticketID);
		await ticketChannel.AddOverwriteAsync(member, Permissions.AccessChannels);

		await ticketChannel.SendMessageAsync("Hello, " + member.Mention + "!\n" + Config.welcomeMessage);

		// Refreshes the channel as changes were made to it above
		ticketChannel = await SupportBoi.discordClient.GetChannelAsync(ticketChannel.Id);

		if (staffID != 0)
		{
			await ticketChannel.SendMessageAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Ticket was randomly assigned to <@" + staffID + ">."
			});

			if (Config.assignmentNotifications)
			{
				try
				{
					DiscordMember staffMember = await category.Guild.GetMemberAsync(staffID);
					await staffMember.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "You have been randomly assigned to a newly opened support ticket: " +
									  ticketChannel.Mention
					});
				}
				catch (NotFoundException) {}
				catch (UnauthorizedException) {}
			}
		}
		
		// Log it if the log channel exists
		DiscordChannel logChannel = category.Guild.GetChannel(Config.logChannel);
		if (logChannel != null)
		{
			DiscordEmbed logMessage = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Ticket " + ticketChannel.Mention + " opened by " + member.Mention + ".\n",
				Footer = new DiscordEmbedBuilder.EmbedFooter {Text = "Ticket " + ticketID}
			};
			await logChannel.SendMessageAsync(logMessage);
		}
		
		return (true, "Ticket opened, " + member.Mention + "!\n" + ticketChannel.Mention);
	}
}
