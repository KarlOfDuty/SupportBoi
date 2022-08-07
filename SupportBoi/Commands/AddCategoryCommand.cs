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
	public async Task OnExecute(InteractionContext command, [Option("Title", "The name to display on buttons and in selection boxes.")] string title, [Option("Category", "The category to add.")] DiscordChannel category)
	{
		if (!category.IsCategory)
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "That channel is not a category."
			}, true);
			return;
		}
		
		if (string.IsNullOrWhiteSpace(title))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Invalid category title specified."
			}, true);
			return;
		}

		if (Database.TryGetCategory(category.Id, out Database.Category _))
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

		if(Database.AddCategory(title, category.Id))
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