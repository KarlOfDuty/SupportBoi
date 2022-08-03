using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class NewCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("new")]
		[SlashCommand("new", "Opens a new ticket.")]
		public async Task OnExecute(InteractionContext command)
		{
			// Check if user is blacklisted
			if (Database.IsBlacklisted(command.User.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You are banned from opening tickets."
				}, true);
				return;
			}

			if (Database.IsOpenTicket(command.Channel.Id))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You cannot use this command in a ticket channel."
				}, true);
				return;
			}

			DiscordChannel category = command.Guild.GetChannel(Config.ticketCategory);
			DiscordChannel ticketChannel;

			try
			{
				ticketChannel = await command.Guild.CreateChannelAsync("ticket", ChannelType.Text, category);
			}
			catch (Exception)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured while creating ticket, " + command.Member.Mention +
								  "!\nIs the channel limit reached in the server or ticket category?"
				}, true);
				return;
			}

			if (ticketChannel == null)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured while creating ticket, " + command.Member.Mention +
								  "!\nIs the channel limit reached in the server or ticket category?"
				}, true);
				return;
			}

			ulong staffID = 0;
			if (Config.randomAssignment)
			{
				staffID = Database.GetRandomActiveStaff(0)?.userID ?? 0;
			}

			long id = Database.NewTicket(command.Member.Id, staffID, ticketChannel.Id);
			string ticketID = id.ToString("00000");
			await ticketChannel.ModifyAsync(modifiedAttributes => modifiedAttributes.Name = "ticket-" + ticketID);
			await ticketChannel.AddOverwriteAsync(command.Member, Permissions.AccessChannels);

			await ticketChannel.SendMessageAsync("Hello, " + command.Member.Mention + "!\n" + Config.welcomeMessage);

			// Refreshes the channel as changes were made to it above
			ticketChannel = command.Guild.GetChannel(ticketChannel.Id);

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
						DiscordMember staffMember = await command.Guild.GetMemberAsync(staffID);
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

			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Ticket opened, " + command.Member.Mention + "!\n" + ticketChannel.Mention
			}, true);

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed logMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket " + ticketChannel.Mention + " opened by " + command.Member.Mention + ".\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter {Text = "Ticket " + ticketID}
				};
				await logChannel.SendMessageAsync(logMessage);
			}
		}
	}
}
