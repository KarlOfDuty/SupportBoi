using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class UnblacklistCommand : BaseCommandModule
	{
		[Command("unblacklist")]
		[Description("Un-blacklists a user from opening tickets.")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			if (!await Utilities.VerifyPermission(command, "unblacklist")) return;

			string[] words = Utilities.ParseIDs(command.RawArgumentString);
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
						await command.RespondAsync(message);
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
							await command.RespondAsync(error);
							continue;
						}

						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Green,
							Description = "Removed " + blacklistedUser.Mention + " from blacklist."
						};
						await command.RespondAsync(message);

						// Log it if the log channel exists
						DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
						if (logChannel != null)
						{
							DiscordEmbed logMessage = new DiscordEmbedBuilder
							{
								Color = DiscordColor.Green,
								Description = blacklistedUser.Mention + " was unblacklisted from opening tickets by " + command.Member.Mention + "."
							};
							await logChannel.SendMessageAsync(logMessage);
						}
					}
					catch (Exception)
					{
						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Error occured while removing " + blacklistedUser.Mention + " from blacklist."
						};
						await command.RespondAsync(message);
						throw;
					}
				}
			}
		}
	}
}
