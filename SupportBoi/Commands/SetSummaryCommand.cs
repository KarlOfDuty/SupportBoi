using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class SetSummaryCommand : BaseCommandModule
	{
		[Command("setsummary")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			if (!await Utilities.VerifyPermission(command, "setsummary")) return;

			ulong channelID = command.Channel.Id;
			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "This channel is not a ticket."
				};
				await command.RespondAsync(error);
				return;
			}

			string summary = command.Message.Content.Replace(Config.prefix + "setsummary", "").Trim();

			using (MySqlConnection c = Database.GetConnection())
			{
				c.Open();
				MySqlCommand update = new MySqlCommand(@"UPDATE tickets SET summary = @summary WHERE channel_id = @channel_id", c);
				update.Parameters.AddWithValue("@summary", summary);
				update.Parameters.AddWithValue("@channel_id", channelID);
				update.Prepare();
				update.ExecuteNonQuery();
				update.Dispose();

				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Summary set."
				};
				await command.RespondAsync(message);
			}
		}
	}
}
