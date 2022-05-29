using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SupportBoi.Commands
{
	public class StatusCommand : BaseCommandModule
	{
		[Command("status")]
		[Cooldown(1, 5, CooldownBucketType.User)]
		public async Task OnExecute(CommandContext command, [RemainingText] string commandArgs)
		{
			if (!await Utilities.VerifyPermission(command, "status")) return;

			long openTickets = Database.GetNumberOfTickets();
			long closedTickets = Database.GetNumberOfClosedTickets();

			DiscordEmbed botInfo = new DiscordEmbedBuilder()
				.WithAuthor("KarlofDuty/SupportBoi @ GitHub", "https://github.com/KarlofDuty/SupportBoi", "https://karlofduty.com/img/tardisIcon.jpg")
				.WithTitle("Bot information")
				.WithColor(DiscordColor.Cyan)
				.AddField("Version:", SupportBoi.GetVersion(), false)
				.AddField("Open tickets:", openTickets + "", true)
				.AddField("Closed tickets:", closedTickets + " ", true);
			await command.RespondAsync(botInfo);
		}
	}
}
