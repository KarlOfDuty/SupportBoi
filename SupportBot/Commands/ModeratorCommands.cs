using System;
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
		public async Task Add(CommandContext command)
		{
			IEnumerable<DiscordRole> roles = command.Member.Roles;

			// Check if the user has permission to use this command.
			if (roles.All(x => x.Id != Config.adminRole || x.Id != Config.moderatorRole))
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
				//TODO: Check if they are members of server
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
				catch (Exception e)
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
	}
}
