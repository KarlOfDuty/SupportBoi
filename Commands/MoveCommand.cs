using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class MoveCommand : ApplicationCommandModule
{
    [SlashRequireGuild]
    [SlashCommand("move", "Moves a ticket to another category.")]
    public async Task OnExecute(InteractionContext command, [Option("Category", "The category to move the ticket to. Only has to be the beginning of the name.")] string category)
    {
        // Check if ticket exists in the database
        if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket _))
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            }, true);
            return;
        }

        if (string.IsNullOrEmpty(category))
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: No category provided."
            }, true);
            return;
        }

        IReadOnlyList<DiscordChannel> channels = await command.Guild.GetChannelsAsync();
        IEnumerable<DiscordChannel> categories = channels.Where(x => x.IsCategory);
        DiscordChannel categoryChannel = categories.FirstOrDefault(x => x.Name.StartsWith(category.Trim(), StringComparison.OrdinalIgnoreCase));

        if (categoryChannel == null)
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Could not find a category by that name."
            }, true);
            return;
        }

        if (command.Channel.Id == categoryChannel.Id)
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: The ticket is already in that category."
            }, true);
            return;
        }

        try
        {
            await command.Channel.ModifyAsync(modifiedAttributes => modifiedAttributes.Parent = categoryChannel);
        }
        catch (UnauthorizedException)
        {
            await command.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Not authorized to move this ticket to that category."
            }, true);
            return;
        }

        await command.CreateResponseAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Ticket was moved to " + categoryChannel.Mention
        });
    }
}