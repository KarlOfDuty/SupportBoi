using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class RandomAssignCommand : BaseCommandModule
	{
		[Command("rassign")]
		[Description("Randomly assigns a staff member to a ticket.")]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArguments)
		{
			if (!await Utilities.VerifyPermission(command, "rassign")) return;

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

			// Get a random staff member who is verified to have the correct role if applicable
			DiscordMember staffMember = await GetRandomVerifiedStaffMember(command, ticket);
			if (staffMember == null)
			{
				return;
			}

			// Attempt to assign the staff member to the ticket
			if (!Database.AssignStaff(ticket, staffMember.Id))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "Error: Failed to assign " + staffMember.Mention + " to ticket."
				};
				await command.RespondAsync(error);
				return;
			}

			// Respond that the command was successful
			DiscordEmbed feedback = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Randomly assigned " + staffMember.Mention + " to ticket."
			};
			await command.RespondAsync(feedback);

			// Send a notification to the staff member if applicable
			if (Config.assignmentNotifications)
			{
				try
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "You have been randomly assigned to a support ticket: " + command.Channel.Mention
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
					Description = staffMember.Mention + " was assigned to " + command.Channel.Mention + " by " + command.Member.Mention + "."
				};
				await logChannel.SendMessageAsync(logMessage);
			}
		}

		private async Task<DiscordMember> GetRandomVerifiedStaffMember(CommandContext command, Database.Ticket ticket)
		{
			if (command.RawArguments.Any()) // An argument was provided, check if this can be parsed into a role
			{
				ulong roleID = 0;

				// Try to parse either discord mention or ID
				string[] parsedMessage = Utilities.ParseIDs(command.RawArgumentString);
				if (!ulong.TryParse(parsedMessage[0], out roleID))
				{
					// Try to find role by name
					roleID = Utilities.GetRoleByName(command.Guild, command.RawArgumentString)?.Id ?? 0;
				}

				// Check if a role was found
				if (roleID == 0)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Could not find a role by that name/ID."
					};
					await command.RespondAsync(error);
					return null;
				}

				// Check if role rassign should override staff's active status
				List<Database.StaffMember> staffMembers = Config.randomAssignRoleOverride
					? Database.GetAllStaff(ticket.assignedStaffID)
					: Database.GetActiveStaff(ticket.assignedStaffID);

				// Randomize the list before checking for roles in order to reduce number of API calls
				staffMembers = Utilities.RandomizeList(staffMembers);

				// Get the first staff member that has the role
				foreach (Database.StaffMember sm in staffMembers)
				{
					try
					{
						DiscordMember verifiedMember = await command.Guild.GetMemberAsync(sm.userID);
						if (verifiedMember?.Roles?.Any(role => role.Id == roleID) ?? false)
						{
							return verifiedMember;
						}
					}
					catch (Exception e)
					{
						command.Client.Logger.Log(LogLevel.Information, e, "Error occured trying to find a staff member in the rassign command.");
					}
				}
			}
			else // No role was specified, any active staff will be picked
			{
				Database.StaffMember staffEntry = Database.GetRandomActiveStaff(ticket.assignedStaffID);
				if (staffEntry == null)
				{
					DiscordEmbed error = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Error: There are no other staff members to choose from."
					};
					await command.RespondAsync(error);
					return null;
				}

				// Get the staff member from discord
				try
				{
					return await command.Guild.GetMemberAsync(staffEntry.userID);
				}
				catch (NotFoundException) { }
			}

			// Send a more generic error if we get to this point and still haven't found the staff member
			DiscordEmbed err = new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error: Could not find an applicable staff member."
			};
			await command.RespondAsync(err);
			return null;
		}
	}
}
