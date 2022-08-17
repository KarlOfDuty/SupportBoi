using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class CreateSelectionBoxPanelCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("createselectionboxpanel", "Creates a selection box which users can use to open new tickets in specific categories.")]
	public async Task OnExecute(InteractionContext command, [Option("Message", "(Optional) The message to show in the selection box.")] string message = null)
	{
		DiscordMessageBuilder builder = new DiscordMessageBuilder()
			.WithContent(" ")
			.AddComponents(await GetSelectComponents(command, message ?? "Open new ticket..."));
		
		await command.Channel.SendMessageAsync(builder);
		await command.CreateResponseAsync(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "Successfully created message, make sure to run this command again if you add new categories to the bot."
		}, true);
	}
	
	public static async Task<List<DiscordSelectComponent>> GetSelectComponents(InteractionContext command, string placeholder)
	{
		List<Database.Category> verifiedCategories = await Utilities.GetVerifiedChannels();

		if (verifiedCategories.Count == 0) return new List<DiscordSelectComponent>();
		
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
			selectionComponents.Add(new DiscordSelectComponent("supportboi_newticketselector" + selectionBoxes, placeholder, categoryOptions, false, 0, 1));
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