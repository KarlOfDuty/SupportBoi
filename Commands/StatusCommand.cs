using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SupportBoi.Commands;

public class StatusCommand : ApplicationCommandModule
{
	[SlashRequireGuild]
	[SlashCommand("status", "Shows bot status and information.")]
	public async Task OnExecute(InteractionContext command)
	{
		long openTickets = Database.GetNumberOfTickets();
		long closedTickets = Database.GetNumberOfClosedTickets();

		DiscordEmbed botInfo = new DiscordEmbedBuilder()
			.WithAuthor("KarlofDuty/SupportBoi @ GitHub", "https://github.com/KarlofDuty/SupportBoi", "https://karlofduty.com/img/tardisIcon.jpg")
			.WithTitle("Bot information")
			.WithColor(DiscordColor.Cyan)
			.AddField("Version:", SupportBoi.GetVersion())
			.AddField("Open tickets:", openTickets + "", true)
			.AddField("Closed tickets:", closedTickets + " ", true);
		await command.CreateResponseAsync(botInfo);
	}
}