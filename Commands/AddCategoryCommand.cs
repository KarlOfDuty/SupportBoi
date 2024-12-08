using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class AddCategoryCommand
{
    [RequireGuild]
    [Command("addcategory")]
    [Description("Adds a category to the ticket bot letting users open tickets in them.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("title")] [Description("The name to display on buttons and in selection boxes.")] string title,
        [Parameter("category")] [Description("The category to add.")] DiscordChannel category)
    {
        if (!category.IsCategory)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That channel is not a category."
            }, true);
            return;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Invalid category title specified."
            }, true);
            return;
        }

        if (Database.TryGetCategory(category.Id, out Database.Category _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That category is already registered."
            }, true);
            return;
        }

        if (Database.TryGetCategory(title, out Database.Category _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "There is already a category with that title."
            }, true);
            return;
        }

        if(Database.AddCategory(title, category.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Category added."
            }, true);
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed adding the category to the database."
            }, true);
        }

        await LogChannel.Success(command.User.Mention + " added `" + category.Name + "` as `" + title + "` to the category list.");
    }
}