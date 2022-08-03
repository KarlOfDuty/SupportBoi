using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands
{
	public class CloseCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("close")]
		[SlashCommand("close", "Closes a ticket.")]
		public async Task OnExecute(InteractionContext command)
		{
			ulong channelID = command.Channel.Id;
			string channelName = command.Channel.Name;

			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(channelID, out Database.Ticket ticket))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				}, true);
				return;
			}

			// Build transcript
			try
			{
				await Transcriber.ExecuteAsync(command.Channel.Id, ticket.id);
			}
			catch (Exception)
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "ERROR: Could not save transcript file. Aborting..."
				});
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
