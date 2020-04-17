﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System;
using System.Threading.Tasks;

namespace SupportBoi.Commands
{
    public class UnblacklistCommand
    {
        [Command("unblacklist")]
        [Description("Un-blacklists a user from opening tickets.")]
        public async Task OnExecute(CommandContext command)
        {
            // Check if the user has permission to use this command.
            if (!Config.HasPermission(command.Member, "unblacklist"))
            {
                DiscordEmbed error = new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "You do not have permission to use this command."
                };
                await command.RespondAsync("", false, error);
                command.Client.DebugLogger.LogMessage(LogLevel.Info, "SupportBoi", "User tried to use the unblacklist command but did not have permission.", DateTime.UtcNow);
                return;
            }

            string[] words = Utilities.ParseIDs(command.RawArgumentString);
            foreach (string word in words)
            {
                if (ulong.TryParse(word, out ulong userId))
                {
                    DiscordUser blacklistedUser = null;
                    try
                    {
                        blacklistedUser = await command.Client.GetUserAsync(userId);
                    }
                    catch (NotFoundException) { }

                    if (blacklistedUser == null)
                    {
                        DiscordEmbed message = new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Description = "Error: Could not find user."
                        };
                        await command.RespondAsync("", false, message);
                        continue;
                    }

                    try
                    {
                        if (!Database.UserLinked.UnBlock(blacklistedUser.Id))
                        {
                            DiscordEmbed error = new DiscordEmbedBuilder
                            {
                                Color = DiscordColor.Red,
                                Description = blacklistedUser.Mention + " is not blacklisted."
                            };
                            await command.RespondAsync("", false, error);
                            continue;
                        }

                        DiscordEmbed message = new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Green,
                            Description = "Removed " + blacklistedUser.Mention + " from blacklist."
                        };
                        await command.RespondAsync("", false, message);

                        // Log it if the log channel exists
                        DiscordChannel logChannel = command.Guild.GetChannel(Config.logChannel);
                        if (logChannel != null)
                        {
                            DiscordEmbed logMessage = new DiscordEmbedBuilder
                            {
                                Color = DiscordColor.Green,
                                Description = blacklistedUser.Mention + " was unblacklisted from opening tickets by " + command.Member.Mention + "."
                            };
                            await logChannel.SendMessageAsync("", false, logMessage);
                        }
                    }
                    catch (Exception)
                    {
                        DiscordEmbed message = new DiscordEmbedBuilder
                        {
                            Color = DiscordColor.Red,
                            Description = "Error occured while removing " + blacklistedUser.Mention + " from blacklist."
                        };
                        await command.RespondAsync("", false, message);
                        throw;
                    }
                }
            }
        }
    }
}
