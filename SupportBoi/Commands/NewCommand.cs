﻿using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class NewCommand : BaseCommandModule
	{
		[Command("new")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "new"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the new command but did not have permission.");
				return;
			}

			// Check if user is blacklisted
			if (Database.IsBlacklisted(command.User.Id))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You are banned from opening tickets."
				};
				await command.RespondAsync(error);
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
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured while creating ticket, " + command.Member.Mention +
					              "!\nIs the channel limit reached in the server or ticket category?"
				};
				await command.RespondAsync(error);
				return;
			}

			if (ticketChannel == null)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error occured while creating ticket, " + command.Member.Mention +
					              "!\nIs the channel limit reached in the server or ticket category?"
				};
				await command.RespondAsync(error);
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
			await ticketChannel.AddOverwriteAsync(command.Member, Permissions.AccessChannels, Permissions.None);

			await ticketChannel.SendMessageAsync("Hello, " + command.Member.Mention + "!\n" + Config.welcomeMessage);

			// Refreshes the channel as changes were made to it above
			ticketChannel = command.Guild.GetChannel(ticketChannel.Id);

			if (staffID != 0)
			{
				DiscordEmbed assignmentMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket was randomly assigned to <@" + staffID + ">."
				};
				await ticketChannel.SendMessageAsync(assignmentMessage);

				if (Config.assignmentNotifications)
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "You have been randomly assigned to a newly opened support ticket: " +
						              ticketChannel.Mention
					};

					try
					{
						DiscordMember staffMember = await command.Guild.GetMemberAsync(staffID);
						await staffMember.SendMessageAsync(message);
					}
					catch (NotFoundException) {}
					catch (UnauthorizedException) {}
				}
			}

			DiscordEmbed response = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Ticket opened, " + command.Member.Mention + "!\n" + ticketChannel.Mention
			};
			await command.RespondAsync(response);

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
