using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	[Description("Moderator commands.")]
	[Hidden]
	[Cooldown(1, 5, CooldownBucketType.User)]
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
					
					// Log it if the log channel exists
					DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
					if (logChannel != null)
					{
						DiscordEmbed logMessage = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Green,
							Description =  mentionedMember.Mention + " was added to " + command.Channel.Mention + " by " + command.Member.Mention + "."
						};
						await logChannel.SendMessageAsync("", false, logMessage);
					}
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
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use command but did not have permission.", DateTime.Now);
				return;
			}

			string[] words = command.Message.Content.Replace("<@","").Replace(">", "").Split();
			foreach (string word in words)
			{
				if (ulong.TryParse(word, out ulong userId))
				{
					DiscordUser blacklistedUser = null;
					try
					{
						blacklistedUser = await command.Client.GetUserAsync(userId);
					}
					catch (NotFoundException) { }

					if (blacklistedUser == null)
					{
						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Error: Could not find user."
						};
						await command.RespondAsync("", false, message);
						continue;
					}

					try
					{
						if (!Database.Blacklist(blacklistedUser.Id, command.User.Id))
						{
							DiscordEmbed error = new DiscordEmbedBuilder
							{
								Color = DiscordColor.Red,
								Description = blacklistedUser.Mention + " is already blacklisted."
							};
							await command.RespondAsync("", false, error);
							continue;
						}
						
						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Green,
							Description = "Blacklisted " + blacklistedUser.Mention + "."
						};
						await command.RespondAsync("", false, message);

						// Log it if the log channel exists
						DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
						if (logChannel != null)
						{
							DiscordEmbed logMessage = new DiscordEmbedBuilder
							{
								Color = DiscordColor.Green,
								Description = blacklistedUser.Mention + " was blacklisted from opening tickets by " + command.Member.Mention + "."
							};
							await logChannel.SendMessageAsync("", false, logMessage);
						}
					}
					catch (Exception)
					{
						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Error occured while blacklisting " + blacklistedUser.Mention + "."
						};
						await command.RespondAsync("", false, message);
						throw;
					}
				}
			}
		}
		[Command("unblacklist")]
		[Description("Un-blacklists a user from opening tickets.")]
		public async Task Unblacklist(CommandContext command)
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
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use command but did not have permission.", DateTime.Now);
				return;
			}

			string[] words = command.Message.Content.Replace("<@", "").Replace(">", "").Split();
			foreach (string word in words)
			{
				if (ulong.TryParse(word, out ulong userId))
				{
					DiscordUser blacklistedUser = null;
					try
					{
						blacklistedUser = await command.Client.GetUserAsync(userId);
					}
					catch (NotFoundException) { }

					if (blacklistedUser == null)
					{
						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Error: Could not find user."
						};
						await command.RespondAsync("", false, message);
						continue;
					}

					try
					{
						if (!Database.Unblacklist(blacklistedUser.Id))
						{
							DiscordEmbed error = new DiscordEmbedBuilder
							{
								Color = DiscordColor.Red,
								Description = blacklistedUser.Mention + " is not blacklisted."
							};
							await command.RespondAsync("", false, error);
							continue;
						}

						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Green,
							Description = "Removed " + blacklistedUser.Mention + " from blacklist."
						};
						await command.RespondAsync("", false, message);

						// Log it if the log channel exists
						DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
						if (logChannel != null)
						{
							DiscordEmbed logMessage = new DiscordEmbedBuilder
							{
								Color = DiscordColor.Green,
								Description = blacklistedUser.Mention + " was unblacklisted from opening tickets by " + command.Member.Mention + "."
							};
							await logChannel.SendMessageAsync("", false, logMessage);
						}
					}
					catch (Exception)
					{
						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Error occured while removing " + blacklistedUser.Mention + " from blacklist."
						};
						await command.RespondAsync("", false, message);
						throw;
					}
				}
			}
		}
	}
}
