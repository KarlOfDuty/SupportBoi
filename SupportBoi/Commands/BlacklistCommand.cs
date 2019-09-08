using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands
{
	public class BlacklistCommand
	{
		[Command("blacklist")]
		[Description("Blacklists a user from opening tickets.")]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "blacklist"))
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
	}
}
