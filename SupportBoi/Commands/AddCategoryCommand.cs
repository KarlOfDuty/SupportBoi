using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class AddCategoryCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[Config.ConfigPermissionCheckAttribute("addcategory")]
	[SlashCommand("addcategory", "Adds a category to the ticket bot letting users open tickets in them.")]
	public async Task OnExecute(InteractionContext command, [Option("Title", "The name to display on buttons and in selection boxes.")] string title, [Option("CategoryID", "The channel ID of the category.")] string categoryID)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Invalid category title specified."
			}, true);
			return;
		}

		if (!ulong.TryParse(categoryID, out ulong channelID))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Invalid category ID specified."
			}, true);
			return;
		}

		if (Database.TryGetCategory(channelID, out Database.Category _))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "That category is already registered."
			}, true);
			return;
		}
		
		if (Database.TryGetCategory(title, out Database.Category _))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "There is already a category with that title."
			}, true);
			return;
		}

		DiscordChannel category = null;
		try
		{
			category = await command.Client.GetChannelAsync(channelID);
		}
		catch (Exception) { /* ignored */ }

		if (category == null)
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Could not find a category by that ID."
			}, true);
			return;
		}

		if (!category.IsCategory)
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "That channel is not a category."
			}, true);
			return;
		}
		
		if(Database.AddCategory(title, command.Member.Id))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Category added."
			}, true);
		}
		else
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error: Failed adding the category to the database."
			}, true);
		}
	}
}