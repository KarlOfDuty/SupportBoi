using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class CreateButtonPanelCommand
{
    [RequireGuild]
    [Command("createbuttonpanel")]
    [Description("Creates a series of buttons which users can use to open new tickets in specific categories.")]
    public async Task OnExecute(SlashCommandContext command)
    {
        DiscordMessageBuilder builder = new DiscordMessageBuilder().WithContent(" ");
        List<Database.Category> verifiedCategories = await Utilities.GetVerifiedCategories();

        if (verifiedCategories.Count == 0)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: No registered categories found. You have to use /addcategory first."
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
                buttonRow.Add(new DiscordButtonComponent(DiscordButtonStyle.Primary, "supportboi_newticketbutton " + verifiedCategories[nrOfButtons].id, verifiedCategories[nrOfButtons].name));
            }
            builder.AddComponents(buttonRow);
        }

        await command.Channel.SendMessageAsync(builder);
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Successfully created message, make sure to run this command again if you add new categories to the bot."
        }, true);

        try
        {
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.User.Mention + " created a new button panel in " + command.Channel.Mention + "."
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not find the log channel.");
        }
    }
}