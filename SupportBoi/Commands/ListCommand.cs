using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SupportBoi.Commands
{
    public class ListCommand
    {
        [Command("list")]
        [Cooldown(1, 5, CooldownBucketType.User)]
        public async Task OnExecute(CommandContext command)
        {
            // Check if the user has permission to use this command.
            if (!Config.HasPermission(command.Member, "list"))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "You do not have permission to use this command."
                };
                await command.RespondAsync("", false, error);
                command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the list command but did not have permission.", DateTime.UtcNow);
                return;
            }

            ulong userID;
            string[] parsedMessage = Utilities.ParseIDs(command.RawArgumentString);

            if (!parsedMessage.Any())
            {
                userID = command.Member.Id;
            }
            else if (!ulong.TryParse(parsedMessage[0], out userID))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Invalid ID/Mention. (Could not convert to numerical)"
                };
                await command.RespondAsync("", false, error);
                return;
            }

            if (Database.TryGetOpenTickets(userID, out List<Database.Ticket> openTickets))
            {
                List<string> listItems = new List<string>();
                foreach (Database.Ticket ticket in openTickets)
                {
                    listItems.Add("**" + ticket.FormattedCreatedTime() + ":** <#" + ticket.channelID + ">\n");
                }

                LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
                foreach (string message in messages)
                {
                    DiscordEmbed channelInfo = new DiscordEmbedBuilder()
                        .WithTitle("Open tickets: ")
                        .WithColor(DiscordColor.Green)
                        .WithDescription(message);
                    await command.RespondAsync("", false, channelInfo);
                }
            }
            else
            {
                DiscordEmbed channelInfo = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithDescription("User does not have any open tickets.");
                await command.RespondAsync("", false, channelInfo);
            }

            if (Database.TryGetClosedTickets(userID, out List<Database.Ticket> closedTickets))
            {
                List<string> listItems = new List<string>();
                foreach (Database.Ticket ticket in closedTickets)
                {
                    listItems.Add("**" + ticket.FormattedCreatedTime() + ":** Ticket " + ticket.id.ToString("00000") + "\n");
                }

                LinkedList<string> messages = Utilities.ParseListIntoMessages(listItems);
                foreach (string message in messages)
                {
                    DiscordEmbed channelInfo = new DiscordEmbedBuilder()
                        .WithTitle("Closed tickets: ")
                        .WithColor(DiscordColor.Red)
                        .WithDescription(message);
                    await command.RespondAsync("", false, channelInfo);
                }
            }
            else
            {
                DiscordEmbed channelInfo = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithDescription("User does not have any closed tickets.");
                await command.RespondAsync("", false, channelInfo);
            }
        }
    }
}
