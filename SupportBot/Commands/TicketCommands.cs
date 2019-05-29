using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SupportBot.Commands
{
	public class TicketCommands
	{
		[Command("new")]
		public async Task New(CommandContext command)
		{
			await command.RespondAsync("Received new command");
		}
	}
}