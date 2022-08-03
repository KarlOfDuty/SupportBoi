using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;

namespace SupportBoi
{
	internal static class EventHandler
	{
		//DateTime for the end of the cooldown
		private static Dictionary<ulong, DateTime> reactionTicketCooldowns = new Dictionary<ulong, DateTime>();

		internal static Task OnReady(DiscordClient client, ReadyEventArgs e)
		{
			Logger.Log(LogID.DISCORD, "Client is ready to process events.");

			// Checking activity type
			if (!Enum.TryParse(Config.presenceType, true, out ActivityType activityType))
			{
				Logger.Log(LogID.CONFIG, "Presence type '" + Config.presenceType + "' invalid, using 'Playing' instead.");
				activityType = ActivityType.Playing;
			}

			client.UpdateStatusAsync(new DiscordActivity(Config.presenceText, activityType), UserStatus.Online);
			return Task.CompletedTask;
		}

		internal static Task OnGuildAvailable(DiscordClient _, GuildCreateEventArgs e)
		{
			Logger.Log(LogID.DISCORD, "Guild available: " + e.Guild.Name);

			IReadOnlyDictionary<ulong, DiscordRole> roles = e.Guild.Roles;

			foreach ((ulong roleID, DiscordRole role) in roles)
			{
				Logger.Log(LogID.DISCORD, role.Name.PadRight(40, '.') + roleID);
			}
			return Task.CompletedTask;
		}

		internal static Task OnClientError(DiscordClient _, ClientErrorEventArgs e)
		{
			Logger.Error(LogID.DISCORD, "Exception occured:\n" + e.Exception);
			return Task.CompletedTask;
		}

		internal static async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
		{
			if (e.Author.IsBot)
			{
				return;
			}

			// Check if ticket exists in the database and ticket notifications are enabled
			if (!Database.TryGetOpenTicket(e.Channel.Id, out Database.Ticket ticket) || !Config.ticketUpdatedNotifications)
			{
				return;
			}

			// Sends a DM to the assigned staff member if at least a day has gone by since the last message and the user sending the message isn't staff
			IReadOnlyList<DiscordMessage> messages = await e.Channel.GetMessagesAsync(2);
			if (messages.Count > 1 && messages[1].Timestamp < DateTimeOffset.UtcNow.AddDays(Config.ticketUpdatedNotificationDelay * -1) && !Database.IsStaff(e.Author.Id))
			{
				try
				{
					DiscordMember staffMember = await e.Guild.GetMemberAsync(ticket.assignedStaffID);
					await staffMember.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "A ticket you are assigned to has been updated: " + e.Channel.Mention
					});
				}
				catch (NotFoundException) { }
				catch (UnauthorizedException) { }
			}
		}

		internal static Task OnCommandError(SlashCommandsExtension commandSystem, SlashCommandErrorEventArgs e)
		{
			switch (e.Exception)
			{
				case SlashExecutionChecksFailedException checksFailedException:
				{
					foreach (SlashCheckBaseAttribute attr in checksFailedException.FailedChecks)
					{
						e.Context?.Channel?.SendMessageAsync(new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = ParseFailedCheck(attr)
						});
					}
					return Task.CompletedTask;
				}

				default:
				{
					Logger.Error(LogID.COMMAND, "Exception occured: " + e.Exception.GetType() + ": " + e.Exception);
					e.Context?.Channel?.SendMessageAsync(new DiscordEmbedBuilder
					{
						Color = DiscordColor.Red,
						Description = "Internal error occured, please report this to the developer."
					});
					return Task.CompletedTask;
				}
			}
		}

		internal static async Task OnMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
		{
			if (!Database.TryGetOpenTickets(e.Member.Id, out List<Database.Ticket> ownTickets))
			{
				return;
			}

			foreach (Database.Ticket ticket in ownTickets)
			{
				try
				{
					DiscordChannel channel = await client.GetChannelAsync(ticket.channelID);
					if (channel?.GuildId == e.Guild.Id)
					{
						await channel.SendMessageAsync(new DiscordEmbedBuilder
						{
							Color = DiscordColor.Green,
							Description = "User '" + e.Member.Username + "#" + e.Member.Discriminator + "' has rejoined the server, and has been re-added to the ticket."
						});
					}
				}
				catch (Exception) { }
			}
		}

		internal static async Task OnMemberRemoved(DiscordClient client, GuildMemberRemoveEventArgs e)
		{
			if (Database.TryGetOpenTickets(e.Member.Id, out List<Database.Ticket> ownTickets))
			{
				foreach(Database.Ticket ticket in ownTickets)
				{
					try
					{
						DiscordChannel channel = await client.GetChannelAsync(ticket.channelID);
						if (channel?.GuildId == e.Guild.Id)
						{
							await channel.SendMessageAsync(new DiscordEmbedBuilder
							{
								Color = DiscordColor.Red,
								Description = "User '" + e.Member.Username + "#" + e.Member.Discriminator + "' has left the server."
							});
						}
					}
					catch (Exception) { }
				}
			}

			if (Database.TryGetAssignedTickets(e.Member.Id, out List<Database.Ticket> assignedTickets) && Config.logChannel != 0)
			{
				DiscordChannel logChannel = await client.GetChannelAsync(Config.logChannel);
				if (logChannel != null)
				{
					foreach (Database.Ticket ticket in assignedTickets)
					{
						try
						{
							DiscordChannel channel = await client.GetChannelAsync(ticket.channelID);
							if (channel?.GuildId == e.Guild.Id)
							{
								await logChannel.SendMessageAsync(new DiscordEmbedBuilder
								{
									Color = DiscordColor.Red,
									Description = "Assigned staff member '" + e.Member.Username + "#" + e.Member.Discriminator + "' has left the server: <#" + channel.Id + ">"
								});
							}
						}
						catch (Exception) { }
					}
				}
			}
		}

		private static string ParseFailedCheck(SlashCheckBaseAttribute attr)
		{
			return attr switch
			{
				SlashRequireDirectMessageAttribute _ => "This command can only be used in direct messages!",
				SlashRequireOwnerAttribute _ => "Only the server owner can use that command!",
				SlashRequirePermissionsAttribute _ => "You don't have permission to do that!",
				SlashRequireBotPermissionsAttribute _ => "The bot doesn't have the required permissions to do that!",
				SlashRequireUserPermissionsAttribute _ => "You don't have permission to do that!",
				SlashRequireGuildAttribute _ => "This command has to be used in a Discord server!",
				Config.ConfigPermissionCheckAttribute _ => "You don't have permission to use this command!",
				_ => "Unknown Discord API error occured, please try again later."
			};
		}
	}
}
