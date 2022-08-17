using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class SummaryCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("summary", "Lists tickets assigned to a user.")]
	public async Task OnExecute(InteractionContext command)
	{
		if (Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
		{
			DiscordEmbed channelInfo = new DiscordEmbedBuilder()
				.WithTitle("Channel information")
				.WithColor(DiscordColor.Cyan)
				.AddField("Ticket number:", ticket.id.ToString("00000"), true)
				.AddField("Ticket creator:", $"<@{ticket.creatorID}>", true)
				.AddField("Assigned staff:", ticket.assignedStaffID == 0 ? "Unassigned." : $"<@{ticket.assignedStaffID}>", true)
				.AddField("Creation time:", ticket.DiscordRelativeTime(), true)
				.AddField("Summary:", string.IsNullOrEmpty(ticket.summary) ? "No summary." : ticket.summary.Replace("\\n", "\n"));
			await command.CreateResponseAsync(channelInfo);
		}
		else
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "This channel is not a ticket."
			}, true);
		}
	}
}