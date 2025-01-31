using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using MySqlConnector;

namespace SupportBoi.Commands;

public class SetSummaryCommand
{
    [RequireGuild]
    [Command("setsummary")]
    [Description("Sets a ticket's summary for the summary command.")]
    public async Task OnExecute(SlashCommandContext command, [Parameter("Summary")] [Description("The ticket summary text.")] string summary)
    {
        // Check if ticket exists in the database
        if (!Database.Ticket.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            });
            return;
        }

        if (Database.Ticket.SetSummary(command.Channel.Id, summary))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "Summary set."
            }, true);

            await LogChannel.Success(command.User.Mention + " set the summary for " + command.Channel.Mention + " to:\n\n" + summary, ticket.id);
        }
        else
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Failed setting the summary of a ticket in database."
            }, true);
        }
    }
}