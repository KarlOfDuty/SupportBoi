using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class SummaryCommand
	{
		[Command("summary")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "summary"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the summary command but did not have permission.", DateTime.Now);
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


				int id = results.GetInt32("id");
				string createdTime = results.GetString("created_time");
				ulong creatorID = results.GetUInt64("creator_id");
				ulong assignedStaffID = results.GetUInt64("assigned_staff_id");
				string summary = results.GetString("summary");

				DiscordEmbed message = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Green)
					.AddField("Time opened", createdTime, true)
					.AddField("Opened by", "<@" + creatorID + ">", true)
					.AddField("Summary", summary, false);
				await command.RespondAsync("", false, message);
			}
		}
	}
}
