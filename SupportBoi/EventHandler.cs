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
using Microsoft.Extensions.Logging;

namespace SupportBoi
{
	internal class EventHandler
	{
		private DiscordClient discordClient;

		//DateTime for the end of the cooldown
		private static Dictionary<ulong, DateTime> reactionTicketCooldowns = new Dictionary<ulong, DateTime>();

		public EventHandler(DiscordClient client)
		{
			this.discordClient = client;
		}

		internal Task OnReady(DiscordClient client, ReadyEventArgs e)
		{
			discordClient.Logger.Log(LogLevel.Information, "Client is ready to process events.");

			// Checking activity type
			if (!Enum.TryParse(Config.presenceType, true, out ActivityType activityType))
			{
				Console.WriteLine("Presence type '" + Config.presenceType + "' invalid, using 'Playing' instead.");
				activityType = ActivityType.Playing;
			}

			this.discordClient.UpdateStatusAsync(new DiscordActivity(Config.presenceText, activityType), UserStatus.Online);
			return Task.CompletedTask;
		}

		internal Task OnGuildAvailable(DiscordClient client, GuildCreateEventArgs e)
		{
			discordClient.Logger.Log(LogLevel.Information, $"Guild available: {e.Guild.Name}");

			IReadOnlyDictionary<ulong, DiscordRole> roles = e.Guild.Roles;

			foreach ((ulong roleID, DiscordRole role) in roles)
			{
				discordClient.Logger.Log(LogLevel.Information, role.Name.PadRight(40, '.') + roleID);
			}
			return Task.CompletedTask;
		}

		internal Task OnClientError(DiscordClient client, ClientErrorEventArgs e)
		{
			discordClient.Logger.Log(LogLevel.Error, $"Exception occured: {e.Exception.GetType()}: {e.Exception}");

			return Task.CompletedTask;
		}

		internal async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
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

		internal Task OnCommandError(CommandsNextExtension commandSystem, CommandErrorEventArgs e)
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
						discordClient.Logger.Log(LogLevel.Error, $"Exception occured: {e.Exception.GetType()}: {e.Exception}");
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

		internal async Task OnReactionAdded(DiscordClient client, MessageReactionAddEventArgs e)
		{
			if (e.Message.Id != Config.reactionMessage) return;

			DiscordGuild guild = e.Message.Channel.Guild;
			DiscordMember member = await guild.GetMemberAsync(e.User.Id);

			if (!Config.HasPermission(member, "new") || Database.IsBlacklisted(member.Id)) return;
			if (reactionTicketCooldowns.ContainsKey(member.Id))
			{
				if (reactionTicketCooldowns[member.Id] > DateTime.Now) return; // cooldown has not expired
				else reactionTicketCooldowns.Remove(member.Id); // cooldown exists but has expired, delete it
			}


			DiscordChannel category = guild.GetChannel(Config.ticketCategory);
			DiscordChannel ticketChannel = await guild.CreateChannelAsync("ticket", ChannelType.Text, category);

			if (ticketChannel == null) return;

			ulong staffID = 0;
			if (Config.randomAssignment)
			{
				staffID = Database.GetRandomActiveStaff(0)?.userID ?? 0;
			}

			long id = Database.NewTicket(member.Id, staffID, ticketChannel.Id);
			reactionTicketCooldowns.Add(member.Id, DateTime.Now.AddSeconds(10)); // add a cooldown which expires in 10 seconds
			string ticketID = id.ToString("00000");

			await ticketChannel.ModifyAsync(model => model.Name = "ticket-" + ticketID);
			await ticketChannel.AddOverwriteAsync(member, Permissions.AccessChannels, Permissions.None);
			await ticketChannel.SendMessageAsync("Hello, " + member.Mention + "!\n" + Config.welcomeMessage);

			// Remove user's reaction
			await e.Message.DeleteReactionAsync(e.Emoji, e.User);

			// Refreshes the channel as changes were made to it above
			ticketChannel = await SupportBoi.GetClient().GetChannelAsync(ticketChannel.Id);

			if (staffID != 0)
			{
				DiscordEmbed assignmentMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket was randomly assigned to <@" + staffID + ">."
				};
				await ticketChannel.SendMessageAsync("", false, assignmentMessage);

				if (Config.assignmentNotifications)
				{
					DiscordEmbed message = new DiscordEmbedBuilder
					{
						Color = DiscordColor.Green,
						Description = "You have been randomly assigned to a newly opened support ticket: " +
						              ticketChannel.Mention
					};

					try
					{
						DiscordMember staffMember = await guild.GetMemberAsync(staffID);
						await staffMember.SendMessageAsync("", false, message);
					}
					catch (NotFoundException)
					{
					}
					catch (UnauthorizedException)
					{
					}
				}
			}

			// Log it if the log channel exists
			DiscordChannel logChannel = guild.GetChannel(Config.logChannel);
			if (logChannel != null)
			{
				DiscordEmbed logMessage = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Green,
					Description = "Ticket " + ticketChannel.Mention + " opened by " + member.Mention + ".\n",
					Footer = new DiscordEmbedBuilder.EmbedFooter {Text = "Ticket " + ticketID}
				};
				await logChannel.SendMessageAsync("", false, logMessage);
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
				case RequireRolesAttribute _:
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
