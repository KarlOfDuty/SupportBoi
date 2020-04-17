using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace SupportBoi.Commands
{
    public class AddCommand
    {
        [Command("add")]
        [Description("Adds a user to a ticket.")]
        public async Task OnExecute(CommandContext command)
        {
            // Check if the user has permission to use this command.
            if (!Config.HasPermission(command.Member, "add"))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "You do not have permission to use this command."
                };
                await command.RespondAsync("", false, error);
                command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the add command but did not have permission.", DateTime.UtcNow);
                return;
            }

            // Check if ticket exists in the database
            if (!Database.TicketLinked.IsOpenTicket(command.Channel.Id))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "This channel is not a ticket."
                };
                await command.RespondAsync("", false, error);
                return;
            }

            string[] parsedArgs = Utilities.ParseIDs(command.RawArgumentString);
            foreach (string parsedArg in parsedArgs)
            {
                if (!ulong.TryParse(parsedArg, out ulong userID))
                {
                    DiscordEmbed error = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = "Invalid ID/Mention. (Could not convert to numerical)"
                    };
                    await command.RespondAsync("", false, error);
                    continue;
                }

                DiscordMember mentionedMember;
                try
                {
                    mentionedMember = await command.Guild.GetMemberAsync(userID);
                }
                catch (Exception)
                {
                    DiscordEmbed error = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = "Invalid ID/Mention. (Could not find user on this server)"
                    };
                    await command.RespondAsync("", false, error);
                    continue;
                }

                try
                {
                    await command.Channel.AddOverwriteAsync(mentionedMember, Permissions.AccessChannels, Permissions.None);
                    DiscordEmbed message = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Green,
                        Description = "Added " + mentionedMember.Mention + " to ticket."
                    };
                    await command.RespondAsync("", false, message);

                    // Log it if the log channel exists
                    DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
                    if (logChannel != null)
                    {
                        DiscordEmbed logMessage = new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Description = mentionedMember.Mention + " was added to " + command.Channel.Mention +
                                          " by " + command.Member.Mention + "."
                        };
                        await logChannel.SendMessageAsync("", false, logMessage);
                    }
                }
                catch (Exception)
                {
                    DiscordEmbed message = new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Description = "Could not add <@" + parsedArg + "> to ticket, unknown error occured."
                    };
                    await command.RespondAsync("", false, message);
                }
            }
        }
    }
}
