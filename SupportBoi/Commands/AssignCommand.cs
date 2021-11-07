using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class AssignCommand : BaseCommandModule
	{
		[Command("assign")]
		[Description("Assigns a staff member to a ticket.")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "assign"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync(error);
				command.Client.Logger.Log(LogLevel.Information, "User tried to use the assign command but did not have permission.");
				return;
			}

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

			ulong staffID;
			string[] parsedMessage = Utilities.ParseIDs(command.RawArgumentString);

			if (!parsedMessage.Any())
			{
				staffID = command.Member.Id;
			}
			else if (!ulong.TryParse(parsedMessage[0], out staffID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Invalid ID/Mention. (Could not convert to numerical)"
				};
				await command.RespondAsync(error);
				return;
			}

			DiscordMember staffMember = null;
			try
			{
				staffMember = await command.Guild.GetMemberAsync(staffID);
			}
			catch (NotFoundException) {  }

			if (staffMember == null)
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Could not find user."
				};
				await command.RespondAsync(error);
				return;
			}

			if (!Database.IsStaff(staffMember.Id))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: User is not registered as staff."
				};
				await command.RespondAsync(error);
				return;
			}

			if (!Database.AssignStaff(ticket, staffID))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed to assign " + staffMember.Mention + " to ticket."
				};
				await command.RespondAsync(error);
				return;
			}

			DiscordEmbed feedback = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Assigned " + staffMember.Mention + " to ticket."
			};
			await command.RespondAsync(feedback);

			if (Config.assignmentNotifications)
			{
				try
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "You have been assigned to a support ticket: " + command.Channel.Mention
					};
					await staffMember.SendMessageAsync(message);
				}
				catch (UnauthorizedException) {}

			}

			// Log it if the log channel exists
			DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed logMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = staffMember.Mention + " was was assigned to " + command.Channel.Mention + " by " + command.Member.Mention + "."
				};
				await logChannel.SendMessageAsync(logMessage);
			}
		}
	}
}
