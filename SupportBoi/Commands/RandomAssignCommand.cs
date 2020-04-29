﻿using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands
{
	public class RandomAssignCommand
	{
		[Command("rassign")]
		[Description("Randomly assigns a staff member to a ticket.")]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "rassign"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the rassign command but did not have permission.", DateTime.Now);
				return;
			}

			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			Database.StaffMember staffEntry = Database.GetRandomActiveStaff(ticket.assignedStaffID);

			if (staffEntry == null)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: There are no other staff to choose from."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			DiscordMember staffMember = null;
			try
			{
				staffMember = await command.Guild.GetMemberAsync(staffEntry.userID);
			}
			catch (NotFoundException) { }

			if (staffMember == null)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Could not find user."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			if (!Database.AssignStaff(ticket, staffEntry.userID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed to assign " + staffMember.Mention + " to ticket."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			DiscordEmbed feedback = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Randomly assigned " + staffMember.Mention + " to ticket."
			};
			await command.RespondAsync("", false, feedback);

			if (Config.assignmentNotifications)
			{
				try
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "You have been randomly assigned to a support ticket: " + command.Channel.Mention
					};
					await staffMember.SendMessageAsync("", false, message);
				}
				catch (UnauthorizedException) {}
			}

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed logMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = staffMember.Mention + " was was assigned to " + command.Channel.Mention + " by " + command.Member.Mention + "."
				};
				await logChannel.SendMessageAsync("", false, logMessage);
			}

			if (Config.sheetsEnabled)
			{
				DiscordMember user = null;
				try
				{
					user = await command.Guild.GetMemberAsync(ticket.creatorID);
				}
				catch (NotFoundException) { }

				Sheets.DeleteTicketQueued(ticket.id);
				Sheets.AddTicketQueued(user, command.Channel, ticket.id.ToString(), staffMember.Id.ToString(), staffMember.DisplayName, ticket.createdTime, null, ticket.summary);
			}
		}
	}
}
