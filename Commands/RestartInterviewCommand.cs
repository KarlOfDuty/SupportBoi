using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi.Commands;

public class RestartInterviewCommand
{
  [Command("restartinterview")]
  [Description("Restarts the automated interview in this ticket, using an updated template if available.")]
  public async Task OnExecute(SlashCommandContext command)
  {
    if (!Database.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
    {
      await command.RespondAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Red,
        Description = "This channel is not a ticket."
      }, true);
      return;
    }

    await command.DeferResponseAsync(true);
    await Interviewer.RestartInterview(command);
    await command.RespondAsync(new DiscordEmbedBuilder
    {
      Color = DiscordColor.Green,
      Description = "Interview restarted."
    }, true);

    try
    {
      DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
      await logChannel.SendMessageAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Green,
        Description = command.User.Mention + " restarted interview in " + command.Channel.Mention + ".",
        Footer = new DiscordEmbedBuilder.EmbedFooter
        {
          Text = "Ticket: " + ticket.id.ToString("00000")
        }
      });
    }
    catch (NotFoundException)
    {
      Logger.Error("Could not find the log channel.");
    }
  }
}