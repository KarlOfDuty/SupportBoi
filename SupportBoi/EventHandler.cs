using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

namespace SupportBoi
{
	internal class EventHandler
	{
		private DiscordClient discordClient;
		public EventHandler(DiscordClient client)
		{
			this.discordClient = client;
		}

		internal Task OnReady(ReadyEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "Client is ready to process events.", DateTime.UtcNow);
			this.discordClient.UpdateStatusAsync(new DiscordGame(Config.prefix + "new"), UserStatus.Online);
			return Task.CompletedTask;
		}

		internal Task OnGuildAvailable(GuildCreateEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", $"Guild available: {e.Guild.Name}", DateTime.UtcNow);

			IReadOnlyList<DiscordRole> roles = e.Guild.Roles;

			foreach (DiscordRole role in roles)
			{
				e.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", role.Name.PadRight(40, '.') + role.Id, DateTime.UtcNow);
			}
			return Task.CompletedTask;
		}

		internal Task OnClientError(ClientErrorEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBoi", $"Exception occured: {e.Exception.GetType()}: {e.Exception}", DateTime.UtcNow);

			return Task.CompletedTask;
		}

		internal async Task OnMessageCreated(MessageCreateEventArgs e)
		{
			if (e.Author.IsBot)
			{
				return;
			}


			// Check if ticket exists in the database
			if (!Database.TryGetOpenTicket(e.Channel.Id, out Database.Ticket ticket))
			{
				return;
			}

			// Updates last staff message sent field in the google sheet
			if (Database.IsStaff(e.Author.Id) && Config.sheetsEnabled)
			{
				Sheets.RefreshLastStaffMessageSentQueued(ticket.id);
			}

			if (!Config.ticketUpdatedNotifications)
			{
				return;
			}

			// Sends a DM to the assigned staff member if at least a day has gone by since the last message and the user sending the message isn't staff
			IReadOnlyList<DiscordMessage> messages = await e.Channel.GetMessagesAsync(2);
			if (messages.Count > 1 && messages[1].Timestamp < DateTimeOffset.UtcNow.AddDays(Config.ticketUpdatedNotificationDelay * -1) && !Database.IsStaff(e.Author.Id))
			{
				try
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "A ticket you are assigned to has been updated: " + e.Channel.Mention
					};

					DiscordMember staffMember = await e.Guild.GetMemberAsync(ticket.assignedStaffID);
					await staffMember.SendMessageAsync("", false, message);
				}
				catch (NotFoundException) { }
				catch (UnauthorizedException) { }
			}
		}

		internal Task OnCommandError(CommandErrorEventArgs e)
		{
			switch (e.Exception)
			{
				case CommandNotFoundException _:
					return Task.CompletedTask;
				case ChecksFailedException _:
					{
						foreach (CheckBaseAttribute attr in ((ChecksFailedException)e.Exception).FailedChecks)
						{
							DiscordEmbed error = new DiscordEmbedBuilder
							{
								Color = DiscordColor.Red,
								Description = this.ParseFailedCheck(attr)
							};
							e.Context?.Channel?.SendMessageAsync("", false, error);
						}
						return Task.CompletedTask;
					}

				default:
					{
						e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "SupportBoi", $"Exception occured: {e.Exception.GetType()}: {e.Exception}", DateTime.UtcNow);
						DiscordEmbed error = new DiscordEmbedBuilder
						{
							Color = DiscordColor.Red,
							Description = "Internal error occured, please report this to the developer."
						};
						e.Context?.Channel?.SendMessageAsync("", false, error);
						return Task.CompletedTask;
					}
			}
		}

		private string ParseFailedCheck(CheckBaseAttribute attr)
		{
			switch (attr)
			{
				case CooldownAttribute _:
					return "You cannot use do that so often!";
				case RequireOwnerAttribute _:
					return "Only the server owner can use that command!";
				case RequirePermissionsAttribute _:
					return "You don't have permission to do that!";
				case RequireRolesAttributeAttribute _:
					return "You do not have a required role!";
				case RequireUserPermissionsAttribute _:
					return "You don't have permission to do that!";
				case RequireNsfwAttribute _:
					return "This command can only be used in an NSFW channel!";
				default:
					return "Unknown Discord API error occured, please try again later.";
			}
		}
	}
}
