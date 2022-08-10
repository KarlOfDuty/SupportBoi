using System;
using System.Collections.Generic;
using System.Linq;
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
		List<Database.Category> verifiedCategories = await Utilities.GetVerifiedChannels();
		switch (verifiedCategories.Count)
		{
			case 0:
				await command.CreateResponseAsync(new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: No registered categories found."
				}, true);
				return;
			case 1:
				await command.DeferAsync(true);
				(bool success, string message) = await OpenNewTicket(command.User.Id, command.Channel.Id, verifiedCategories[0].id);
		
				if (success)
				{
					await command.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = message
					}).AsEphemeral());
				}
				else
				{
					await command.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = message
					}).AsEphemeral());
				}
				return;
			default:
				if (Config.newCommandUsesSelector)
				{
					await CreateSelector(command, verifiedCategories);
				}
				else
				{
					await CreateButtons(command, verifiedCategories);
				}
				return;
		}
	}

	public static async Task CreateButtons(InteractionContext command, List<Database.Category> verifiedCategories)
	{
		DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder().WithContent(" ");
		int nrOfButtons = 0;
		for (int nrOfButtonRows = 0; nrOfButtonRows < 5 && nrOfButtons < verifiedCategories.Count; nrOfButtonRows++)
		{
			List<DiscordButtonComponent> buttonRow = new List<DiscordButtonComponent>();
			
			for (; nrOfButtons < 5 * (nrOfButtonRows + 1) && nrOfButtons < verifiedCategories.Count; nrOfButtons++)
			{
				buttonRow.Add(new DiscordButtonComponent(ButtonStyle.Primary, "supportboi_newcommandbutton " + verifiedCategories[nrOfButtons].id, verifiedCategories[nrOfButtons].name));
			}
			builder.AddComponents(buttonRow);
		}
		
		await command.CreateResponseAsync(builder.AsEphemeral());
	}
	
	public static async Task CreateSelector(InteractionContext command, List<Database.Category> verifiedCategories)
	{
		verifiedCategories = verifiedCategories.OrderBy(x => x.name).ToList();
		List<DiscordSelectComponent> selectionComponents = new List<DiscordSelectComponent>();
		int selectionOptions = 0;
		for (int selectionBoxes = 0; selectionBoxes < 5 && selectionOptions < verifiedCategories.Count; selectionBoxes++)
		{
			List<DiscordSelectComponentOption> categoryOptions = new List<DiscordSelectComponentOption>();
			
			for (; selectionOptions < 25 * (selectionBoxes + 1) && selectionOptions < verifiedCategories.Count; selectionOptions++)
			{
				categoryOptions.Add(new DiscordSelectComponentOption(verifiedCategories[selectionOptions].name, verifiedCategories[selectionOptions].id.ToString()));
			}
			selectionComponents.Add(new DiscordSelectComponent("supportboi_newcommandselector" + selectionBoxes, "Open new ticket...", categoryOptions, false, 0, 1));
		}
				
		await command.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddComponents(selectionComponents).AsEphemeral());
	}
	
	public static async Task OnCategorySelection(DiscordInteraction interaction)
	{
		string stringID = "";
		switch (interaction.Data.ComponentType)
		{
			case ComponentType.Button:
				stringID = interaction.Data.CustomId.Replace("supportboi_newcommandbutton ", "");
				break;
			case ComponentType.Select:
				if (interaction.Data.Values == null || interaction.Data.Values.Length <= 0) return;
				stringID = interaction.Data.Values[0];
				break;
			default:
				return;
		}
		
		if (!ulong.TryParse(stringID, out ulong categoryID) || categoryID == 0) return;
		
		await interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate, new DiscordInteractionResponseBuilder().AsEphemeral());

		(bool success, string message) = await NewCommand.OpenNewTicket(interaction.User.Id, interaction.ChannelId, categoryID);

		if (success)
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = message
			}));
		}
		else
		{
			await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = message
			}));
		}
	}
	
	public static async Task<(bool, string)> OpenNewTicket(ulong userID, ulong commandChannelID, ulong categoryID)
	{
		// Check if user is blacklisted
		if (Database.IsBlacklisted(userID))
		{
			return (false, "You are banned from opening tickets.");
		}

		if (Database.IsOpenTicket(commandChannelID))
		{
			return (false, "You cannot use this command in a ticket channel.");
		}

		DiscordChannel category = null;
		try
		{
			category = await SupportBoi.discordClient.GetChannelAsync(categoryID);
		}
		catch (Exception) { /*ignored*/ }

		if (category == null)
		{
			return (false, "Error: Could not find the category to place the ticket in.");
		}

		DiscordMember member = null;
		try
		{
			member = await category.Guild.GetMemberAsync(userID);
		}
		catch (Exception) { /*ignored*/ }

		if (member == null)
		{
			return (false, "Error: Could not find you on the Discord server.");
		}
		
		DiscordChannel ticketChannel;

		try
		{
			ticketChannel = await category.Guild.CreateChannelAsync("ticket", ChannelType.Text, category);
		}
		catch (Exception)
		{
			return (false, "Error occured while creating ticket, " + member.Mention + 
						   "!\nIs the channel limit reached in the server or ticket category?");
		}

		if (ticketChannel == null)
		{
			return (false, "Error occured while creating ticket, " + member.Mention +
						   "!\nIs the channel limit reached in the server or ticket category?");
		}

		ulong staffID = 0;
		if (Config.randomAssignment)
		{
			staffID = Database.GetRandomActiveStaff(0)?.userID ?? 0;
		}

		long id = Database.NewTicket(member.Id, staffID, ticketChannel.Id);
		string ticketID = id.ToString("00000");
		await ticketChannel.ModifyAsync(modifiedAttributes => modifiedAttributes.Name = "ticket-" + ticketID);
		await ticketChannel.AddOverwriteAsync(member, Permissions.AccessChannels);

		await ticketChannel.SendMessageAsync("Hello, " + member.Mention + "!\n" + Config.welcomeMessage);

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
		
		// Log it if the log channel exists
		DiscordChannel logChannel = category.Guild.GetChannel(Config.logChannel);
		if (logChannel != null)
		{
			DiscordEmbed logMessage = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Ticket " + ticketChannel.Mention + " opened by " + member.Mention + ".\n",
				Footer = new DiscordEmbedBuilder.EmbedFooter {Text = "Ticket " + ticketID}
			};
			await logChannel.SendMessageAsync(logMessage);
		}
		
		return (true, "Ticket opened, " + member.Mention + "!\n" + ticketChannel.Mention);
	}
}
