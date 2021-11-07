﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class CloseCommand :BaseCommandModule
	{
		[Command("close")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "close"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the close command but did not have permission.");
				return;
			}

			ulong channelID = command.Channel.Id;
			string channelName = command.Channel.Name;

			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(channelID, out Database.Ticket ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync(error);
				return;
			}

			// Build transcript
			try
			{
				await Transcriber.ExecuteAsync(command.Channel.Id.ToString(), ticket.id);
			}
			catch (Exception)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "ERROR: Could not save transcript file. Aborting..."
				};
				await command.RespondAsync(error);
				throw;
			}

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed embed = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket " + ticket.id.ToString("00000") + " closed by " + command.Member.Mention + ".\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
				};

				using (FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read))
				{
					DiscordMessageBuilder message = new DiscordMessageBuilder();
					message.WithEmbed(embed);
					message.WithFiles(new Dictionary<string, Stream>() { { Transcriber.GetFilename(ticket.id), file } });

					await logChannel.SendMessageAsync(message);
				}
			}

			if (Config.closingNotifications)
			{
				DiscordEmbed embed = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket " + ticket.id.ToString("00000") + " which you opened has now been closed, check the transcript for more info.\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
				};

				try
				{
					DiscordMember staffMember = await command.Guild.GetMemberAsync(ticket.creatorID);

					using (FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read))
					{
						DiscordMessageBuilder message = new DiscordMessageBuilder();
						message.WithEmbed(embed);
						message.WithFiles(new Dictionary<string, Stream>() { { Transcriber.GetFilename(ticket.id), file } });

						await staffMember.SendMessageAsync(message);
					}
				}
				catch (NotFoundException) { }
				catch (UnauthorizedException) { }
			}

			Database.ArchiveTicket(ticket);

			// Delete the channel and database entry
			await command.Channel.DeleteAsync("Ticket closed.");

			Database.DeleteOpenTicket(ticket.id);
		}
	}
}
