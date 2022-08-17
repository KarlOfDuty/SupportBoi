using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class CloseCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("close", "Closes a ticket.")]
	public async Task OnExecute(InteractionContext command)
	{
		// Check if ticket exists in the database
		if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket _))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "This channel is not a ticket."
			});
			return;
		}

		DiscordInteractionResponseBuilder confirmation = new DiscordInteractionResponseBuilder()
			.AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Cyan,
				Description = "Are you sure you wish to close this ticket? You cannot re-open it again later."
			})
			.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, "supportboi_closeconfirm", "Confirm"));
			

		await command.CreateResponseAsync(confirmation);
	}

	public static async Task OnConfirmed(DiscordInteraction interaction)
	{
		await interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
		ulong channelID = interaction.Channel.Id;
		string channelName = interaction.Channel.Name;
			
		// Check if ticket exists in the database
		if (!Database.TryGetOpenTicket(channelID, out Database.Ticket ticket))
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "This channel is not a ticket."
			}));
			return;
		}
			
		// Build transcript
		try
		{
			await Transcriber.ExecuteAsync(interaction.Channel.Id, ticket.id);
		}
		catch (Exception e)
		{
			Logger.Error("Exception occured when trying to save transcript while closing ticket: " + e);
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "ERROR: Could not save transcript file. Aborting..."
			}));
			return;
		}

		// Log it if the log channel exists
		DiscordChannel logChannel = interaction.Guild.GetChannel(Config.logChannel);
		if (logChannel != null)
		{
			DiscordEmbed embed = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Ticket " + ticket.id.ToString("00000") + " closed by " + interaction.User.Mention + ".\n",
				Footer = new DiscordEmbedBuilder.EmbedFooter { Text = '#' + channelName }
			};

			await using FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read);
			DiscordMessageBuilder message = new DiscordMessageBuilder();
			message.WithEmbed(embed);
			message.WithFiles(new Dictionary<string, Stream> { { Transcriber.GetFilename(ticket.id), file } });

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
				DiscordMember staffMember = await interaction.Guild.GetMemberAsync(ticket.creatorID);
				await using FileStream file = new FileStream(Transcriber.GetPath(ticket.id), FileMode.Open, FileAccess.Read);
					
				DiscordMessageBuilder message = new DiscordMessageBuilder();
				message.WithEmbed(embed);
				message.WithFiles(new Dictionary<string, Stream> { { Transcriber.GetFilename(ticket.id), file } });

				await staffMember.SendMessageAsync(message);
			}
			catch (NotFoundException) { }
			catch (UnauthorizedException) { }
		}
			
		Database.ArchiveTicket(ticket);

		await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "Channel will be deleted in 3 seconds..."
		}));


			await Task.Delay(3000);

		// Delete the channel and database entry
		await interaction.Channel.DeleteAsync("Ticket closed.");

		Database.DeleteOpenTicket(ticket.id);
	}
}