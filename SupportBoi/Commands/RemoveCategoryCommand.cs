using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class RemoveCategoryCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("removecategory", "Removes the ability for users to open tickets in a specific category.")]
	public async Task OnExecute(InteractionContext command, [Option("Category", "The category to remove.")] DiscordChannel channel)
	{
		if (!Database.TryGetCategory(channel.Id, out Database.Category category))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "That category is not registered."
			}, true);
			return;
		}

		if (Database.RemoveCategory(channel.Id))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Category removed."
			}, true);
		}
		else
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error: Failed removing the category from the database."
			}, true);
		}
	}
}