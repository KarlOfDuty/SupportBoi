using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SupportBoi.Commands
{
    public class MoveCommand
    {
        [Command("move")]
        [Description("Moves a ticket to another category.")]
        public async Task OnExecute(CommandContext command)
        {
            // Check if the user has permission to use this command.
            if (!Config.HasPermission(command.Member, "move"))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "You do not have permission to use this command."
                };
                await command.RespondAsync("", false, error);
                command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the move command but did not have permission.", DateTime.UtcNow);
                return;
            }

            // Check if ticket exists in the database
            if (!Database.TicketLinked.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "This channel is not a ticket."
                };
                await command.RespondAsync("", false, error);
                return;
            }

            if (string.IsNullOrEmpty(command.RawArgumentString))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Error: No category provided."
                };
                await command.RespondAsync("", false, error);
                return;
            }

            IReadOnlyList<DiscordChannel> channels = await command.Guild.GetChannelsAsync();
            IEnumerable<DiscordChannel> categories = channels.Where(x => x.IsCategory);
            DiscordChannel category = categories.FirstOrDefault(x => x.Name.StartsWith(command.RawArgumentString.Trim(), StringComparison.OrdinalIgnoreCase));

            if (category == null)
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Error: Could not find a category by that name."
                };
                await command.RespondAsync("", false, error);
                return;
            }

            if (command.Channel.Id == category.Id)
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Error: The ticket is already in that category."
                };
                await command.RespondAsync("", false, error);
                return;
            }

            try
            {
                await command.Channel.ModifyAsync(null, null, null, category, null, null, null);
            }
            catch (UnauthorizedException)
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Error: Not authorized to move this ticket to that category."
                };
                await command.RespondAsync("", false, error);
                return;
            }

            DiscordEmbed feedback = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Ticket was moved to " + category.Mention
            };
            await command.RespondAsync("", false, feedback);
        }
    }
}
