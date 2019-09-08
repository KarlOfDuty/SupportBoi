﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SupportBoi.Commands
{
	public class AssignCommand
	{
		[Command("assign")]
		[Description("Assigns a staff member to a ticket.")]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "assign"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use command but did not have permission.", DateTime.Now);
				return;
			}

			// Check if ticket exists in the database
			if (!Database.IsTicket(command.Channel.Id))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync("", false, error);
				return;
			}

			IReadOnlyList<DiscordUser> mentionedUsers = command.Message.MentionedUsers;

			foreach (DiscordUser mentionedUser in mentionedUsers)
			{
				try
				{
					DiscordMember mentionedMember = await command.Guild.GetMemberAsync(mentionedUser.Id);


					// TODO: Add ticket assigning to mysql database and implement google sheets api
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "Assigned " + mentionedMember.Mention + " to ticket."
					};
					await command.RespondAsync("", false, message);

					// Log it if the log channel exists
					DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
					if (logChannel != null)
					{
						DiscordEmbed logMessage = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Green,
							Description = mentionedMember.Mention + " was assigned to " + command.Channel.Mention + " by " + command.Member.Mention + "."
						};
						await logChannel.SendMessageAsync("", false, logMessage);
					}
				}
				catch (Exception)
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Could not assign " + mentionedUser.Mention + " to ticket, they were not found on this server."
					};
					await command.RespondAsync("", false, message);
					throw;
				}

			}
		}
	}
}
