using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
				});
				return;
			}

			DiscordInteractionResponseBuilder dfmb = new DiscordInteractionResponseBuilder()
				.AddEmbed(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Cyan,
					Description = "Are you sure you wish to close this ticket? You cannot re-open it again later."
				})
				.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, "supportboi_closeconfirm", "Confirm"));
			

			await command.CreateResponseAsync(dfmb);
		}

		public static async Task OnConfirmed(DiscordClient client, ComponentInteractionCreateEventArgs buttonEvent)
		{
			await buttonEvent.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
			ulong channelID = buttonEvent.Channel.Id;
			string channelName = buttonEvent.Channel.Name;
			
			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(channelID, out Database.Ticket ticket))
			{
				await buttonEvent.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				}));
				return;
			}
			
			// Build transcript
			try
			{
				await Transcriber.ExecuteAsync(buttonEvent.Channel.Id, ticket.id);
			}
			catch (Exception)
			{
				await buttonEvent.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "ERROR: Could not save transcript file. Aborting..."
				}));
				return;
			}

			// Log it if the log channel exists
			DiscordChannel logChannel = buttonEvent.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed embed = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket " + ticket.id.ToString("00000") + " closed by " + buttonEvent.User.Mention + ".\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
				};

				await using FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read);
				DiscordMessageBuilder message = new DiscordMessageBuilder();
				message.WithEmbed(embed);
				message.WithFiles(new Dictionary<string, Stream>() { { Transcriber.GetFilename(ticket.id), file } });

				await logChannel.SendMessageAsync(message);
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
					DiscordMember staffMember = await buttonEvent.Guild.GetMemberAsync(ticket.creatorID);
					await using FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read);
					
					DiscordMessageBuilder message = new DiscordMessageBuilder();
					message.WithEmbed(embed);
					message.WithFiles(new Dictionary<string, Stream>() { { Transcriber.GetFilename(ticket.id), file } });

					await staffMember.SendMessageAsync(message);
				}
				catch (NotFoundException) { }
				catch (UnauthorizedException) { }
			}
			
			Database.ArchiveTicket(ticket);

			await buttonEvent.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Channel will be deleted in 3 seconds..."
			}));

			await Task.Delay(3000);

			// Delete the channel and database entry
			await buttonEvent.Channel.DeleteAsync("Ticket closed.");

			Database.DeleteOpenTicket(ticket.id);
		}
	}
}
