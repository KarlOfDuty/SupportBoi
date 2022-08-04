using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class SetSummaryCommand : ApplicationCommandModule
	{
		[SlashRequireGuild]
		[Config.ConfigPermissionCheckAttribute("setsummary")]
		[SlashCommand("setsummary", "Sets a ticket's summary for the summary command.")]
		public async Task OnExecute(InteractionContext command, [Option("Summary", "The ticket summary text.")] string summary)
		{
			ulong channelID = command.Channel.Id;
			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				await command.CreateResponseAsync(new DiscordEmbedBuilder
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
			update.Prepare();
			update.ExecuteNonQuery();
			update.Dispose();
			
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Summary set."
			}, true);
		}
	}
}
