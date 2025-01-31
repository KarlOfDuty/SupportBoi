using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class RemoveCategoryCommand
{
    [RequireGuild]
    [Command("removecategory")]
    [Description("Removes the ability for users to open tickets in a specific category.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("category")] [Description("The category to remove.")] DiscordChannel category)
    {
        if (!Database.Category.TryGetCategory(category.Id, out Database.Category _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That category is not registered."
            }, true);
            return;
        }

        if (Database.Category.RemoveCategory(category.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Category removed."
            }, true);
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed removing the category from the database."
            }, true);
        }

        await LogChannel.Success("`" + category.Name + "` was removed by " + command.User.Mention + ".");
    }
}