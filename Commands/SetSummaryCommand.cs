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
        ulong channelID = command.Channel.Id;
        // Check if ticket exists in the database
        if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
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

        try
        {
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.User.Mention + " set the summary for " + command.Channel.Mention + " to:\n\n" + summary,
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "Ticket: " + ticket.id.ToString("00000")
                }
            });
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not send message in log channel.");
        }
    }
}