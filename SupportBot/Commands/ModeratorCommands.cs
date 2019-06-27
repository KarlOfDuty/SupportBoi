﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBot.Commands
{
	[Description("Moderator commands.")]
	[Hidden]
	[Cooldown(1, 10, CooldownBucketType.User)]
	public class ModeratorCommands
	{
		[Command("add")]
		[Description("Adds a user to a ticket.")]
		public async Task Add(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.IsModerator(command.Member.Roles))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBot", "User tried to use command but did not have permission.", DateTime.Now);
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

			IReadOnlyList <DiscordUser> mentionedUsers = command.Message.MentionedUsers;

			foreach (DiscordUser mentionedUser in mentionedUsers)
			{
				try
				{
					DiscordMember mentionedMember = await command.Guild.GetMemberAsync(mentionedUser.Id);
					await command.Channel.AddOverwriteAsync(mentionedMember, Permissions.AccessChannels, Permissions.None);
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "Added " + mentionedMember.Mention + " to ticket."
					};
					await command.RespondAsync("", false, message);
				}
				catch (Exception)
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Could not add " + mentionedUser.Mention + " to ticket, they were not found on this server."
					};
					await command.RespondAsync("", false, message);
					throw;
				}

			}
			
		}
		[Command("blacklist")]
		[Description("Blacklists a user from opening tickets.")]
		public async Task Blacklist(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.IsModerator(command.Member.Roles))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBot", "User tried to use command but did not have permission.", DateTime.Now);
				return;
			}

			IReadOnlyList<DiscordUser> mentionedUsers = command.Message.MentionedUsers;

			foreach (DiscordUser mentionedUser in mentionedUsers)
			{
				try
				{
					Database.Blacklist(mentionedUser.Id, command.User.Id);
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "Blacklisted " + mentionedUser.Mention + "."
					};
					await command.RespondAsync("", false, message);
				}
				catch (Exception)
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Error occured while blacklisting " + mentionedUser.Mention + "."
					};
					await command.RespondAsync("", false, message);
					throw;
				}

			}

		}
	}
}
