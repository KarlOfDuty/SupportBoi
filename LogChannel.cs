using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace SupportBoi;

public static class LogChannel
{
  public static bool IsEnabled => Config.logChannel != 0;

  public static async Task Log(string message, uint ticketID = 0, Utilities.File file = null)
  {
    await Log(DiscordColor.Cyan, message, ticketID, file);
  }

  public static async Task Success(string message, uint ticketID = 0, Utilities.File file = null)
  {
    await Log(DiscordColor.Green, message, ticketID, file);
  }

  public static async Task Warn(string message, uint ticketID = 0, Utilities.File file = null)
  {
    await Log(DiscordColor.Orange, message, ticketID, file);
  }

  public static async Task Error(string message, uint ticketID = 0, Utilities.File file = null)
  {
    await Log(DiscordColor.Red, message, ticketID, file);
  }

  private static async Task Log(DiscordColor color, string message, uint ticketID = 0, Utilities.File file = null)
  {
    if (ticketID != 0)
    {
      Logger.Log("Log Channel: (ticket-" + ticketID.ToString("00000") + ") " + message.Trim());
    }
    else
    {
      Logger.Log("Log Channel: " + message.Trim());
    }

    if (!IsEnabled)
    {
      return;
    }

    try
    {
      DiscordEmbedBuilder embedBuilder = new()
      {
        Color = color,
        Description = message
      };
      if (ticketID != 0)
      {
        embedBuilder.WithFooter("Ticket: " + ticketID.ToString("00000"));
      }

      DiscordMessageBuilder messageBuilder = new();
      messageBuilder.AddEmbed(embedBuilder);
      if (file != null)
      {
        messageBuilder.AddFile(file.fileName, file.contents);
      }

      DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
      await logChannel.SendMessageAsync(messageBuilder);
    }
    catch (NotFoundException)
    {
      Logger.Error("Log channel does not exist. Channel ID: " + Config.logChannel);
    }
    catch (UnauthorizedException)
    {
      Logger.Error("No permissions to send message in log channel. Channel ID: " + Config.logChannel);
    }
    catch (Exception e)
    {
      Logger.Error("Error occured trying to send message in log channel. Channel ID: " + Config.logChannel, e);
    }
  }
}