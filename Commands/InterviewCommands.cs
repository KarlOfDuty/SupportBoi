using System.ComponentModel;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using SupportBoi.Interviews;

namespace SupportBoi.Commands;

[Command("interview")]
[Description("Interview management.")]
public class InterviewCommands
{
  [Command("restart")]
  [Description("Restarts the interview in this ticket, using an updated template if available.")]
  public async Task Restart(SlashCommandContext command)
  {
    if (!Database.Ticket.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
    {
      await command.RespondAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Red,
        Description = "This channel is not a ticket."
      }, true);
      return;
    }

    await command.DeferResponseAsync(true);

    if (await Interviewer.RestartInterview(command.Channel))
    {
      await command.RespondAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Green,
        Description = "Interview restarted."
      }, true);
    }
    else
    {
      await command.RespondAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Red,
        Description = "An error occured when trying to restart the interview."
      }, true);
    }

    await LogChannel.Success(command.User.Mention + " restarted interview in " + command.Channel.Mention + ".", ticket.id);
  }

  [Command("stop")]
  [Description("Stops the interview in this ticket.")]
  public async Task Stop(SlashCommandContext command)
  {
    if (!Database.Ticket.TryGetOpenTicket(command.Channel.Id, out Database.Ticket ticket))
    {
      await command.RespondAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Red,
        Description = "This channel is not a ticket."
      }, true);
      return;
    }

    if (!Database.Interviews.TryGetInterview(command.Channel.Id, out Interview _))
    {
      await command.RespondAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Red,
        Description = "There is no interview open in this ticket.."
      }, true);
      return;
    }

    await command.DeferResponseAsync(true);

    if (await Interviewer.StopInterview(command.Channel))
    {
      await command.RespondAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Green,
        Description = "Interview stopped."
      }, true);
    }
    else
    {
      await command.RespondAsync(new DiscordEmbedBuilder
      {
        Color = DiscordColor.Red,
        Description = "An error occured when trying to stop the interview."
      }, true);
    }

    await LogChannel.Success(command.User.Mention + " stopped the interview in " + command.Channel.Mention + ".", ticket.id);
  }
}