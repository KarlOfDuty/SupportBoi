using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using MySqlConnector;

namespace SupportBoi.Commands;

public class SetSummaryCommand
{
    [RequireGuild]
    [Command("setsummary")]
    [Description("Sets a ticket's summary for the summary command.")]
    public async Task OnExecute(SlashCommandContext command, [Parameter("Summary")] [Description("The ticket summary text.")] string summary)
    {
        ulong channelID = command.Channel.Id;
        // Check if ticket exists in the database
        if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "This channel is not a ticket."
            });
            return;
        }

        await using MySqlConnection c = Database.GetConnection();
        c.Open();
        MySqlCommand update = new MySqlCommand(@"UPDATE tickets SET summary = @summary WHERE channel_id = @channel_id", c);
        update.Parameters.AddWithValue("@summary", summary);
        update.Parameters.AddWithValue("@channel_id", channelID);
        await update.PrepareAsync();
        update.ExecuteNonQuery();
        update.Dispose();

        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Summary set."
        }, true);
    }
}