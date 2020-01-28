using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SupportBoi.Commands
{
	public class ListOldestCommand
	{
		[Command("listoldest")]
		[Aliases("lo")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command)
		{
			// Check if the user has permission to use this command.
			if (!Config.HasPermission(command.Member, "listoldest"))
			{
				DiscordEmbed error = new DiscordEmbedBuilder
				{
					Color = DiscordColor.Red,
					Description = "You do not have permission to use this command."
				};
				await command.RespondAsync("", false, error);
				command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the listoldest command but did not have permission.", DateTime.UtcNow);
				return;
			}

			if (Database.TryGetOldestTickets(command.Member.Id, out List<Database.Ticket> openTickets))
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithTitle("The oldest open tickets: ")
					.WithColor(DiscordColor.Green)
					.WithDescription(string.Join(", ", openTickets.Select(x => "<#" + x.channelID + ">")));
				await command.RespondAsync("", false, channelInfo);
			}
			else
			{
				DiscordEmbed channelInfo = new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Red)
					.WithDescription("Could not fetch any open tickets.");
				await command.RespondAsync("", false, channelInfo);
			}
		}
	}
}
