using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class UnassignCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("unassign", "Unassigns a staff member from a ticket.")]
	public async Task OnExecute(InteractionContext command)
	{
		// Check if ticket exists in the database
		if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "This channel is not a ticket."
			}, true);
			return;
		}

		if (!Database.UnassignStaff(ticket))
		{
			await command.CreateResponseAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Red,
				Description = "Error: Failed to unassign staff member from ticket."
			}, true);
			return;
		}

		await command.CreateResponseAsync(new DiscordEmbedBuilder
		{
			Color = DiscordColor.Green,
			Description = "Unassigned staff member from ticket."
		});

		// Log it if the log channel exists
		DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
		if (logChannel != null)
		{
			await logChannel.SendMessageAsync(new DiscordEmbedBuilder
			{
				Color = DiscordColor.Green,
				Description = "Staff member was unassigned from " + command.Channel.Mention + " by " + command.Member.Mention + "."
			});
		}
	}
}