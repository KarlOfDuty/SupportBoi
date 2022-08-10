using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Tyrrrz.Extensions;

namespace SupportBoi.Commands;

public class CreateSelectionBoxPanelCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("createselectionboxpanel", "Creates a selection box which users can use to open new tickets in specific categories.")]
	public async Task OnExecute(InteractionContext command, [Option("Message", "(Optional) The message to show above the selection box (required by Discord API).")] string message = null)
	{
		DiscordMessageBuilder builder = new DiscordMessageBuilder()
			.WithContent(message ?? "Open a new support ticket here:")
			.AddComponents(await GetSelectComponents(command));;
		
		await command.Channel.SendMessageAsync(builder);
		await command.CreateResponseAsync(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "Successfully created message, make sure to run this command again if you add new categories to the bot."
		}, true);
	}
	
	public static async Task<List<DiscordSelectComponent>> GetSelectComponents(InteractionContext command)
	{
		List<Database.Category> categories = Database.GetAllCategories();
		List<Database.Category> verifiedCategories = new List<Database.Category>();

		foreach (Database.Category category in categories)
		{
			DiscordChannel channel = null;
			try
			{
				channel = await command.Client.GetChannelAsync(category.id);
			}
			catch (Exception) { /*ignored*/ }

			if (channel != null)
			{
				verifiedCategories.Add(category);
			}
			else
			{
				Logger.Warn("Category '" + category.name + "' (" + category.id + ") no longer exists! Ignoring...");
			}
		}
		
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
			selectionComponents.Add(new DiscordSelectComponent("supportboi_newticketselector" + selectionBoxes, "Open new ticket...", categoryOptions, false, 0, 1));
		}

		return selectionComponents;
	}

	public static async Task OnSelectionMenuUsed(DiscordInteraction interaction)
	{
		if (interaction.Data.Values == null || interaction.Data.Values.Length <= 0) return;
		
		if (!ulong.TryParse(interaction.Data.Values[0], out ulong categoryID) || categoryID == 0) return;

		await interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());

		(bool success, string message) = await NewCommand.OpenNewTicket(interaction.User.Id, interaction.ChannelId, categoryID);

		if (success)
		{
			await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = message
			}).AsEphemeral());
		}
		else
		{
			await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = message
			}).AsEphemeral());
		}
	}
}