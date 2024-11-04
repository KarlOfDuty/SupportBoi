using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace SupportBoi.Commands;

public class RestartInterviewCommand
{
  [Command("restartinterview")]
  [Description("Restarts the automated interview in this channel, using an updated template if available.")]
  public async Task OnExecute(SlashCommandContext command)
  {
    await command.DeferResponseAsync(true);
    await Interviewer.RestartInterview(command);
    await command.RespondAsync(new DiscordEmbedBuilder
    {
      Color = DiscordColor.Green,
      Description = "Interview restarted."
    }, true);
  }
}