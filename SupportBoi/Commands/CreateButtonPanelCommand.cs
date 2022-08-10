using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class CreateButtonPanelCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("createbuttonpanel", "Creates a series of buttons which users can use to open new tickets in specific categories.")]
	public async Task OnExecute(InteractionContext command)
	{
		DiscordMessageBuilder builder = new DiscordMessageBuilder().WithContent(" ");
		List<Database.Category> verifiedCategories = await Utilities.GetVerifiedChannels();

		if (verifiedCategories.Count == 0)
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error: No registered categories found."
			}, true);
			return;
		}
		
		verifiedCategories = verifiedCategories.OrderBy(x => x.name).ToList();
		
		int nrOfButtons = 0;
		for (int nrOfButtonRows = 0; nrOfButtonRows < 5 && nrOfButtons < verifiedCategories.Count; nrOfButtonRows++)
		{
			List<DiscordButtonComponent> buttonRow = new List<DiscordButtonComponent>();
			
			for (; nrOfButtons < 5 * (nrOfButtonRows + 1) && nrOfButtons < verifiedCategories.Count; nrOfButtons++)
			{
				buttonRow.Add(new DiscordButtonComponent(ButtonStyle.Primary, "supportboi_newticketbutton " + verifiedCategories[nrOfButtons].id, verifiedCategories[nrOfButtons].name));
			}
			builder.AddComponents(buttonRow);
		}
		
		await command.Channel.SendMessageAsync(builder);
		await command.CreateResponseAsync(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "Successfully created message, make sure to run this command again if you add new categories to the bot."
		}, true);
	}
	
	public static async Task OnButtonUsed(DiscordInteraction interaction)
	{
		await interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		
		if (!ulong.TryParse(interaction.Data.CustomId.Replace("supportboi_newticketbutton ", ""), out ulong categoryID) || categoryID == 0)
		{
			Logger.Warn("Invalid ID: " + interaction.Data.CustomId.Replace("supportboi_newticketbutton ", ""));
			return;
		}
		
		(bool success, string message) = await NewCommand.OpenNewTicket(interaction.User.Id, interaction.ChannelId, categoryID);

		if (success)
		{
			await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = message
			}));
		}
		else
		{
			await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = message
			}));
		}
	}
}