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

public class CreateSelectionBoxPanelCommand
{
    [RequireGuild]
    [Command("createselectionboxpanel")]
    [Description("Creates a selection box which users can use to open new tickets in specific categories.")]
    public async Task OnExecute(SlashCommandContext command,
        [Parameter("placeholder")] [Description("(Optional) The message to show in the selection box.")] string message = null)
    {
        DiscordMessageBuilder builder = new DiscordMessageBuilder()
            .WithContent(" ")
            .AddComponents(await GetSelectComponents(command, message ?? "Open new ticket..."));

        await command.Channel.SendMessageAsync(builder);
        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Successfully created message, make sure to run this command again if you add new categories to the bot."
        }, true);

        await LogChannel.Success(command.User.Mention + " created a new selector panel in " + command.Channel.Mention + ".");
    }

    public static async Task<List<DiscordSelectComponent>> GetSelectComponents(SlashCommandContext command, string placeholder)
    {
        List<Database.Category> verifiedCategories = await Utilities.GetVerifiedCategories();

        if (verifiedCategories.Count == 0)
        {
            return new List<DiscordSelectComponent>();
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
            selectionComponents.Add(new DiscordSelectComponent("supportboi_newticketselector" + selectionBoxes, placeholder, categoryOptions, false, 0, 1));
        }

        return selectionComponents;
    }

    public static async Task OnSelectionMenuUsed(DiscordInteraction interaction)
    {
        if (interaction.Data.Values == null || interaction.Data.Values.Length <= 0)
        {
            return;
        }

        if (!ulong.TryParse(interaction.Data.Values[0], out ulong categoryID) || categoryID == 0)
        {
            return;
        }

        await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());

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