using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class BlacklistCommand : BaseCommandModule
	{
		[Command("blacklist")]
		[Description("Blacklists a user from opening tickets.")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "blacklist"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the blacklist command but did not have permission.");
				return;
			}

			string[] parsedArgs = Utilities.ParseIDs(command.RawArgumentString);
			foreach (string parsedArg in parsedArgs)
			{
				if (ulong.TryParse(parsedArg, out ulong userId))
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
						if (!Database.Blacklist(blacklistedUser.Id, command.User.Id))
						{
							DiscordEmbed error = new DiscordEmbedBuilder
							{
								Color = DiscordColor.Red,
								Description = blacklistedUser.Mention + " is already blacklisted."
							};
							await command.RespondAsync(error);
							continue;
						}

						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Green,
							Description = "Blacklisted " + blacklistedUser.Mention + "."
						};
						await command.RespondAsync(message);

						// Log it if the log channel exists
						DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
						if (logChannel != null)
						{
							DiscordEmbed logMessage = new DiscordEmbedBuilder
							{
								Color = DiscordColor.Green,
								Description = blacklistedUser.Mention + " was blacklisted from opening tickets by " + command.Member.Mention + "."
							};
							await logChannel.SendMessageAsync(logMessage);
						}
					}
					catch (Exception)
					{
						DiscordEmbed message = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Error occured while blacklisting " + blacklistedUser.Mention + "."
						};
						await command.RespondAsync(message);
						throw;
					}
				}
			}
		}
	}
}
