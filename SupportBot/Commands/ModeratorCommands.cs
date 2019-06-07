using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SupportBot.Commands
{
	[Description("Moderator commands.")]
	[Hidden]
	[RequirePermissions(Permissions.ManageChannels)]
	public class ModeratorCommands
	{
		[Command("add")]
		public async Task Add(CommandContext command)
		{
			await command.RespondAsync("Received add command");
		}
	}
}
