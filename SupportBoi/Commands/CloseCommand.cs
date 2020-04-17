using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System;
using System.Threading.Tasks;

namespace SupportBoi.Commands
{
	public class CloseCommand
	{
		[Command("close")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "close"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the close command but did not have permission.", DateTime.UtcNow);
				return;
			}

			ulong channelID = command.Channel.Id;
			string channelName = command.Channel.Name;

			// Check if ticket exists in the database
			if (!Database.TicketLinked.TryGetOpenTicket(channelID, out Database.Ticket ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync("", false, error);
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
				await command.RespondAsync("", false, error);
				throw;
			}
			string filePath = Transcriber.GetPath(ticket.id);

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed logMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket " + ticket.id.ToString("00000") + " closed by " + command.Member.Mention + ".\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
				};
				await logChannel.SendFileAsync(filePath, "", false, logMessage);
			}

			if (Config.closingNotifications)
			{
				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket " + ticket.id.ToString("00000") + " which you opened has now been closed, check the transcript for more info.\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
				};

				try
				{
					DiscordMember staffMember = await command.Guild.GetMemberAsync(ticket.creatorID);
					await staffMember.SendFileAsync(filePath, "", false, message);
				}
				catch (NotFoundException) { }
				catch (UnauthorizedException) { }
			}

			Database.TicketLinked.CloseTicket(ticket);
			await command.Channel.DeleteAsync("Ticket closed.");
			Database.TicketLinked.DeleteOpenTicket(ticket);
			Sheets.DeleteTicketQueued(ticket.id);
		}
	}
}
