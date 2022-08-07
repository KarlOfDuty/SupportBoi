using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;
public class NewCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[Config.ConfigPermissionCheckAttribute("new")]
	[SlashCommand("new", "Opens a new ticket.")]
	public async Task OnExecute(InteractionContext command)
	{
		await command.DeferAsync(true);
		await OpenNewTicket(command.Interaction, Config.ticketCategory);
	}

	public static async Task OpenNewTicket(DiscordInteraction interaction, ulong categoryID)
	{
		//await interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
		
		// Check if user is blacklisted
		if (Database.IsBlacklisted(interaction.User.Id))
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "You are banned from opening tickets."
			}));
			return;
		}

		if (Database.IsOpenTicket(interaction.Channel.Id))
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "You cannot use this command in a ticket channel."
			}));
			return;
		}

		DiscordChannel category = null;
		try
		{
			category = await SupportBoi.discordClient.GetChannelAsync(categoryID);
		}
		catch (Exception) { /*ignored*/ }

		if (category == null)
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error: Could not find the category to place the ticket in."
			}));
			return;
		}

		DiscordMember member = null;
		try
		{
			member = await category.Guild.GetMemberAsync(interaction.User.Id);
		}
		catch (Exception) { /*ignored*/ }

		if (member == null)
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error: Could not find you on the Discord server."
			}));
			return;
		}
		
		DiscordChannel ticketChannel;

		try
		{
			ticketChannel = await category.Guild.CreateChannelAsync("ticket", ChannelType.Text, category);
		}
		catch (Exception)
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error occured while creating ticket, " + interaction.User.Mention +
							  "!\nIs the channel limit reached in the server or ticket category?"
			}));
			return;
		}

		if (ticketChannel == null)
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error occured while creating ticket, " + interaction.User.Mention +
							  "!\nIs the channel limit reached in the server or ticket category?"
			}));
			return;
		}

		ulong staffID = 0;
		if (Config.randomAssignment)
		{
			staffID = Database.GetRandomActiveStaff(0)?.userID ?? 0;
		}

		long id = Database.NewTicket(interaction.User.Id, staffID, ticketChannel.Id);
		string ticketID = id.ToString("00000");
		await ticketChannel.ModifyAsync(modifiedAttributes => modifiedAttributes.Name = "ticket-" + ticketID);
		await ticketChannel.AddOverwriteAsync(member, Permissions.AccessChannels);

		await ticketChannel.SendMessageAsync("Hello, " + interaction.User.Mention + "!\n" + Config.welcomeMessage);

		// Refreshes the channel as changes were made to it above
		ticketChannel = await SupportBoi.discordClient.GetChannelAsync(ticketChannel.Id);

		if (staffID != 0)
		{
			await ticketChannel.SendMessageAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Ticket was randomly assigned to <@" + staffID + ">."
			});

			if (Config.assignmentNotifications)
			{
				try
				{
					DiscordMember staffMember = await category.Guild.GetMemberAsync(staffID);
					await staffMember.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "You have been randomly assigned to a newly opened support ticket: " +
									  ticketChannel.Mention
					});
				}
				catch (NotFoundException) {}
				catch (UnauthorizedException) {}
			}
		}

		await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "Ticket opened, " + interaction.User.Mention + "!\n" + ticketChannel.Mention
		}));

		// Log it if the log channel exists
		DiscordChannel logChannel = category.Guild.GetChannel(Config.logChannel);
		if (logChannel != null)
		{
			DiscordEmbed logMessage = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Ticket " + ticketChannel.Mention + " opened by " + interaction.User.Mention + ".\n",
				Footer = new DiscordEmbedBuilder.EmbedFooter {Text = "Ticket " + ticketID}
			};
			await logChannel.SendMessageAsync(logMessage);
		}
	}
}
