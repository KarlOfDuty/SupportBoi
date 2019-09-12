using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class SetSummaryCommand
	{
		[Command("setsummary")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "setsummary"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the setsummary command but did not have permission.", DateTime.Now);
					return;
				}

				ulong channelID = command.Channel.Id;
				string channelName = command.Channel.Name;
				c.Open();
				MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE channel_id=@channel_id", c);
				selection.Parameters.AddWithValue("@channel_id", channelID);
				selection.Prepare();
				MySqlDataReader results = selection.ExecuteReader();

				// Check if ticket exists in the database
				if (!results.Read())
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "This channel is not a ticket."
					};
					await command.RespondAsync("", false, error);
					return;
				}
				results.Close();

				string summary = command.Message.Content.Remove(0, (Config.prefix + "setsummary ").Length);

				MySqlCommand update = new MySqlCommand(@"UPDATE tickets SET summary = @summary WHERE channel_id = @channel_id", c);
				update.Parameters.AddWithValue("@summary", summary);
				update.Parameters.AddWithValue("@channel_id", channelID);
				update.Prepare();
				update.ExecuteNonQuery();

				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Summary set."
				};
				await command.RespondAsync("", false, message);
			}
		}
	}
}
