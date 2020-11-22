using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace SupportBoi.Commands
{
	public class SetTicketCommand :BaseCommandModule
	{
		[Command("setticket")]
		[Description("Turns a channel into a ticket, warning: this will let anyone with write access delete the channel using the close command.")]
		public async Task OnExecute(CommandContext command)
		{
			using (MySqlConnection c = Database.GetConnection())
			{
				// Check if the user has permission to use this command.
				if (!Config.HasPermission(command.Member, "setticket"))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "You do not have permission to use this command."
					};
					await command.RespondAsync("", false, error);
					command.Client.Logger.Log(LogLevel.Information, "User tried to use the setticket command but did not have permission.");
					return;
				}

				// Check if ticket exists in the database
				if (Database.IsOpenTicket(command.Channel.Id))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "This channel is already a ticket."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				ulong userID;
				string[] parsedMessage = Utilities.ParseIDs(command.RawArgumentString);

				if (!parsedMessage.Any())
				{
					userID = command.Member.Id;
				}
				else if (!ulong.TryParse(parsedMessage[0], out userID))
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Invalid ID/Mention. (Could not convert to numerical)"
					};
					await command.RespondAsync("", false, error);
					return;
				}

				DiscordUser user = await command.Client.GetUserAsync(userID);

				if (user == null)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Invalid ID/Mention."
					};
					await command.RespondAsync("", false, error);
					return;
				}

				long id = Database.NewTicket(userID, 0, command.Channel.Id);
				string ticketID = id.ToString("00000");
				DiscordEmbed message = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Channel has been designated ticket " + ticketID + "."
				};
				await command.RespondAsync("", false, message);

				// Log it if the log channel exists
				DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
				if (logChannel != null)
				{
					DiscordEmbed logMessage = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = command.Channel.Mention + " has been designated ticket " + ticketID + " by " + command.Member.Mention + "."
					};
					await logChannel.SendMessageAsync("", false, logMessage);
				}
			}
		}
	}
}
