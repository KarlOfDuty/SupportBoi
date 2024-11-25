using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;

namespace SupportBoi.Interviews;

public static class Interviewer
{
    public static async Task<bool> StartInterview(DiscordChannel channel)
    {
        if (!Database.TryGetInterviewTemplate(channel.Parent.Id, out InterviewQuestion template))
        {
            return false;
        }

        await CreateQuestion(channel, template);
        return Database.SaveInterview(channel.Id, template);
    }

    public static async Task<bool> RestartInterview(DiscordChannel channel)
    {
        if (Database.TryGetInterview(channel.Id, out InterviewQuestion interviewRoot))
        {
            if (Config.deleteMessagesAfterNoSummary)
            {
                await DeletePreviousMessages(interviewRoot, channel);
            }

            if (!Database.TryDeleteInterview(channel.Id))
            {
                Logger.Warn("Could not delete interview from database. Channel ID: " + channel.Id);
            }
        }

        return await StartInterview(channel);
    }

    public static async Task<bool> StopInterview(DiscordChannel channel)
    {
        if (Database.TryGetInterview(channel.Id, out InterviewQuestion interviewRoot))
        {
            if (Config.deleteMessagesAfterNoSummary)
            {
                await DeletePreviousMessages(interviewRoot, channel);
            }

            if (!Database.TryDeleteInterview(channel.Id))
            {
                Logger.Warn("Could not delete interview from database. Channel ID: " + channel.Id);
            }
        }

        return true;
    }

    public static async Task ProcessButtonOrSelectorResponse(DiscordInteraction interaction)
    {
        if (interaction?.Channel == null || interaction?.Message == null)
        {
            return;
        }

        // Ignore if option was deselected.
        if (interaction.Data.ComponentType is not DiscordComponentType.Button && interaction.Data.Values.Length == 0)
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
            return;
        }

        // Return if there is no active interview in this channel
        if (!Database.TryGetInterview(interaction.Channel.Id, out InterviewQuestion interviewRoot))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Error: There is no active interview in this ticket, ask an admin to check the bot logs if this seems incorrect."))
                .AsEphemeral());
            return;
        }

        // Return if the current question cannot be found in the interview.
        if (!interviewRoot.TryGetCurrentQuestion(out InterviewQuestion currentQuestion))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Error: Something seems to have broken in this interview, you may want to restart it."))
                .AsEphemeral());
            Logger.Error("The interview for channel " + interaction.Channel.Id + " exists but does not have a message ID set for it's root question");
            return;
        }

        // Check if this button/selector is for an older question.
        if (interaction.Message.Id != currentQuestion.messageID)
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Error: You have already replied to this question, you have to reply to the latest one."))
                .AsEphemeral());
            return;
        }

        try
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage);
        }
        catch (Exception e)
        {
            Logger.Error("Could not update original message:", e);
        }

        // Parse the response index from the button/selector.
        string componentID = "";
        string answer = "";

        switch (interaction.Data.ComponentType)
        {
            case DiscordComponentType.UserSelect:
            case DiscordComponentType.RoleSelect:
            case DiscordComponentType.ChannelSelect:
            case DiscordComponentType.MentionableSelect:
                if (interaction.Data.Resolved?.Roles?.Any() ?? false)
                {
                    answer = interaction.Data.Resolved.Roles.First().Value.Mention;
                }
                else if (interaction.Data.Resolved?.Users?.Any() ?? false)
                {
                    answer = interaction.Data.Resolved.Users.First().Value.Mention;
                }
                else if (interaction.Data.Resolved?.Channels?.Any() ?? false)
                {
                    answer = interaction.Data.Resolved.Channels.First().Value.Mention;
                }
                else if (interaction.Data.Resolved?.Messages?.Any() ?? false)
                {
                    answer = interaction.Data.Resolved.Messages.First().Value.Id.ToString();
                }
                break;
            case DiscordComponentType.StringSelect:
                componentID = interaction.Data.Values[0];
                break;
            case DiscordComponentType.Button:
                componentID = interaction.Data.CustomId.Replace("supportboi_interviewbutton ", "");
                break;
            case DiscordComponentType.ActionRow:
            case DiscordComponentType.FormInput:
            default:
                throw new ArgumentOutOfRangeException("Tried to process an invalid component type: " + interaction.Data.ComponentType);
        }

        // The different mentionable selectors provide the actual answer, while the others just return the ID.
        if (componentID == "")
        {
            foreach (KeyValuePair<string, InterviewQuestion> path in currentQuestion.paths)
            {
                // Skip to the first matching path.
                if (Regex.IsMatch(answer, path.Key))
                {
                    await HandleAnswer(answer, path.Value, interviewRoot, currentQuestion, interaction.Channel);
                    return;
                }
            }

            Logger.Error("The interview for channel " + interaction.Channel.Id + " reached a question of type " + currentQuestion.type + " which has no valid next question. Their selection was:\n" + answer);
            DiscordMessage followupMessage = await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Could not determine the next question based on your answer. Check your response and ask an admin to check the bot logs if this seems incorrect."
            }).AsEphemeral());
            currentQuestion.AddRelatedMessageIDs(followupMessage.Id);
            Database.SaveInterview(interaction.Channel.Id, interviewRoot);
        }
        else
        {
            if (!int.TryParse(componentID, out int pathIndex))
            {
                Logger.Error("Invalid interview button/selector index: " + componentID);
                return;
            }

            if (pathIndex >= currentQuestion.paths.Count || pathIndex < 0)
            {
                Logger.Error("Invalid interview button/selector index: " + pathIndex);
                return;
            }

            (string questionString, InterviewQuestion nextQuestion) = currentQuestion.paths.ElementAt(pathIndex);
            await HandleAnswer(questionString, nextQuestion, interviewRoot, currentQuestion, interaction.Channel);
        }
    }

    public static async Task ProcessResponseMessage(DiscordMessage answerMessage)
    {
        // Either the message or the referenced message is null.
        if (answerMessage.Channel == null || answerMessage.ReferencedMessage?.Channel == null)
        {
            return;
        }

        // The channel does not have an active interview.
        if (!Database.TryGetInterview(answerMessage.ReferencedMessage.Channel.Id, out InterviewQuestion interviewRoot))
        {
            return;
        }

        if (!interviewRoot.TryGetCurrentQuestion(out InterviewQuestion currentQuestion))
        {
            return;
        }

        // The user responded to something other than the latest interview question.
        if (answerMessage.ReferencedMessage.Id != currentQuestion.messageID)
        {
            return;
        }

        // The user responded to a question which does not take a text response.
        if (currentQuestion.type != QuestionType.TEXT_INPUT)
        {
            return;
        }

        // The length requirement is less than 1024 characters, and must be less than the configurable limit if it is set.
        int maxLength = Math.Min(currentQuestion.maxLength ?? 1024, 1024);

        if (answerMessage.Content.Length > maxLength)
        {
            DiscordMessage lengthMessage = await answerMessage.RespondAsync(new DiscordEmbedBuilder
            {
                Description = "Error: Your answer cannot be more than " + maxLength + " characters (" + answerMessage.Content.Length + "/" + maxLength + ").",
                Color = DiscordColor.Red
            });
            currentQuestion.AddRelatedMessageIDs(answerMessage.Id, lengthMessage.Id);
            Database.SaveInterview(answerMessage.Channel.Id, interviewRoot);
            return;
        }

        if (answerMessage.Content.Length < (currentQuestion.minLength ?? 0))
        {
            DiscordMessage lengthMessage = await answerMessage.RespondAsync(new DiscordEmbedBuilder
            {
                Description = "Error: Your answer must be at least " + currentQuestion.minLength + " characters (" + answerMessage.Content.Length + "/" + currentQuestion.minLength + ").",
                Color = DiscordColor.Red
            });
            currentQuestion.AddRelatedMessageIDs(answerMessage.Id, lengthMessage.Id);
            Database.SaveInterview(answerMessage.Channel.Id, interviewRoot);
            return;
        }

        foreach ((string questionString, InterviewQuestion nextQuestion) in currentQuestion.paths)
        {
            // Skip to the first matching path.
            if (!Regex.IsMatch(answerMessage.Content, questionString))
            {
                continue;
            }

            await HandleAnswer(answerMessage.Content, nextQuestion, interviewRoot, currentQuestion, answerMessage.Channel, answerMessage);
            return;
        }

        Logger.Error("The interview for channel " + answerMessage.Channel.Id + " reached a question of type " + currentQuestion.type + " which has no valid next question. Their message was:\n" + answerMessage.Content);
        DiscordMessage errorMessage =await answerMessage.RespondAsync(new DiscordEmbedBuilder
        {
            Description = "Error: Could not determine the next question based on your answer. Check your response and ask an admin to check the bot logs if this seems incorrect.",
            Color = DiscordColor.Red
        });
        currentQuestion.AddRelatedMessageIDs(answerMessage.Id, errorMessage.Id);
        Database.SaveInterview(answerMessage.Channel.Id, interviewRoot);
    }

    private static async Task HandleAnswer(string answer,
                                           InterviewQuestion nextQuestion,
                                           InterviewQuestion interviewRoot,
                                           InterviewQuestion previousQuestion,
                                           DiscordChannel channel,
                                           DiscordMessage answerMessage = null)
    {
        // The error message type should not alter anything about the interview.
        if (nextQuestion.type != QuestionType.ERROR)
        {
            previousQuestion.answer = answer;

            // There is no message ID if the question is not a text input.
            previousQuestion.answerID = answerMessage == null ? 0 : answerMessage.Id;
        }

        // Create next question, or finish the interview.
        switch (nextQuestion.type)
        {
            case QuestionType.TEXT_INPUT:
            case QuestionType.BUTTONS:
            case QuestionType.TEXT_SELECTOR:
            case QuestionType.ROLE_SELECTOR:
            case QuestionType.USER_SELECTOR:
            case QuestionType.CHANNEL_SELECTOR:
            case QuestionType.MENTIONABLE_SELECTOR:
                await CreateQuestion(channel, nextQuestion);
                Database.SaveInterview(channel.Id, interviewRoot);
                break;
            case QuestionType.END_WITH_SUMMARY:
                OrderedDictionary summaryFields = new OrderedDictionary();
                interviewRoot.GetSummary(ref summaryFields);

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                {
                    Color = Utilities.StringToColor(nextQuestion.color),
                    Title = nextQuestion.title,
                    Description = nextQuestion.message,
                };

                foreach (DictionaryEntry entry in summaryFields)
                {
                    embed.AddField((string)entry.Key, (string)entry.Value);
                }

                await channel.SendMessageAsync(embed);

                if (Config.deleteMessagesAfterSummary)
                {
                    await DeletePreviousMessages(interviewRoot, channel);
                }

                if (!Database.TryDeleteInterview(channel.Id))
                {
                    Logger.Error("Could not delete interview from database. Channel ID: " + channel.Id);
                }
                return;
            case QuestionType.END_WITHOUT_SUMMARY:
                await channel.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Color = Utilities.StringToColor(nextQuestion.color),
                    Title = nextQuestion.title,
                    Description = nextQuestion.message
                });

                if (Config.deleteMessagesAfterNoSummary)
                {
                    await DeletePreviousMessages(interviewRoot, channel);
                }

                if (!Database.TryDeleteInterview(channel.Id))
                {
                    Logger.Error("Could not delete interview from database. Channel ID: " + channel.Id);
                }
                break;
            case QuestionType.ERROR:
            default:
                if (answerMessage == null)
                {
                    DiscordMessage errorMessage = await channel.SendMessageAsync(new DiscordEmbedBuilder()
                    {
                        Color = Utilities.StringToColor(nextQuestion.color),
                        Title = nextQuestion.title,
                        Description = nextQuestion.message
                    });
                    previousQuestion.AddRelatedMessageIDs(errorMessage.Id);
                }
                else
                {
                    DiscordMessageBuilder errorMessageBuilder = new DiscordMessageBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                        {
                            Color = Utilities.StringToColor(nextQuestion.color),
                            Title = nextQuestion.title,
                            Description = nextQuestion.message
                        }).WithReply(answerMessage.Id);
                    DiscordMessage errorMessage = await answerMessage.RespondAsync(errorMessageBuilder);
                    previousQuestion.AddRelatedMessageIDs(errorMessage.Id, answerMessage.Id);
                }

                Database.SaveInterview(channel.Id, interviewRoot);
                break;
        }
    }

    private static async Task DeletePreviousMessages(InterviewQuestion interviewRoot, DiscordChannel channel)
    {
        List<ulong> previousMessages = [];
        interviewRoot.GetMessageIDs(ref previousMessages);

        foreach (ulong previousMessageID in previousMessages)
        {
            try
            {
                DiscordMessage previousMessage = await channel.GetMessageAsync(previousMessageID);
                await channel.DeleteMessageAsync(previousMessage, "Deleting old interview message.");
            }
            catch (Exception e)
            {
                Logger.Warn("Failed to delete old interview message: ", e);
            }
        }
    }

    private static async Task CreateQuestion(DiscordChannel channel, InterviewQuestion question)
    {
        DiscordMessageBuilder msgBuilder = new();
        DiscordEmbedBuilder embed = new()
        {
            Color = Utilities.StringToColor(question.color),
            Title = question.title,
            Description = question.message
        };

        switch (question.type)
        {
            case QuestionType.BUTTONS:
                int nrOfButtons = 0;
                for (int nrOfButtonRows = 0; nrOfButtonRows < 5 && nrOfButtons < question.paths.Count; nrOfButtonRows++)
                {
                    List<DiscordButtonComponent> buttonRow = [];
                    for (; nrOfButtons < 5 * (nrOfButtonRows + 1) && nrOfButtons < question.paths.Count; nrOfButtons++)
                    {
                        (string questionString, InterviewQuestion nextQuestion) = question.paths.ToArray()[nrOfButtons];
                        buttonRow.Add(new DiscordButtonComponent(nextQuestion.GetButtonStyle(), "supportboi_interviewbutton " + nrOfButtons, questionString));
                    }
                    msgBuilder.AddComponents(buttonRow);
                }
                break;
            case QuestionType.TEXT_SELECTOR:
                List<DiscordSelectComponent> selectionComponents = [];

                int selectionOptions = 0;
                for (int selectionBoxes = 0; selectionBoxes < 5 && selectionOptions < question.paths.Count; selectionBoxes++)
                {
                    List<DiscordSelectComponentOption> categoryOptions = [];
                    for (; selectionOptions < 25 * (selectionBoxes + 1) && selectionOptions < question.paths.Count; selectionOptions++)
                    {
                        (string questionString, InterviewQuestion nextQuestion) = question.paths.ToArray()[selectionOptions];
                        categoryOptions.Add(new DiscordSelectComponentOption(questionString, selectionOptions.ToString(), nextQuestion.selectorDescription));
                    }

                    selectionComponents.Add(new DiscordSelectComponent("supportboi_interviewselector " + selectionBoxes, string.IsNullOrWhiteSpace(question.selectorPlaceholder)
                                                                                                                           ? "Select an option..." : question.selectorPlaceholder, categoryOptions));
                }

                msgBuilder.AddComponents(selectionComponents);
                break;
            case QuestionType.ROLE_SELECTOR:
                msgBuilder.AddComponents(new DiscordRoleSelectComponent("supportboi_interviewroleselector", string.IsNullOrWhiteSpace(question.selectorPlaceholder)
                                                                                                              ? "Select a role..." : question.selectorPlaceholder));
                break;
            case QuestionType.USER_SELECTOR:
                msgBuilder.AddComponents(new DiscordUserSelectComponent("supportboi_interviewuserselector", string.IsNullOrWhiteSpace(question.selectorPlaceholder)
                                                                                                              ? "Select a user..." : question.selectorPlaceholder));
                break;
            case QuestionType.CHANNEL_SELECTOR:
                msgBuilder.AddComponents(new DiscordChannelSelectComponent("supportboi_interviewchannelselector", string.IsNullOrWhiteSpace(question.selectorPlaceholder)
                                                                                                                    ? "Select a channel..." : question.selectorPlaceholder));
                break;
            case QuestionType.MENTIONABLE_SELECTOR:
                msgBuilder.AddComponents(new DiscordMentionableSelectComponent("supportboi_interviewmentionableselector", string.IsNullOrWhiteSpace(question.selectorPlaceholder)
                                                                                                                            ? "Select a user or role..." : question.selectorPlaceholder));
                break;
            case QuestionType.TEXT_INPUT:
                embed.WithFooter("Reply to this message with your answer. You cannot include images or files.");
                break;
            case QuestionType.END_WITH_SUMMARY:
            case QuestionType.END_WITHOUT_SUMMARY:
            case QuestionType.ERROR:
            default:
                break;
        }

        msgBuilder.AddEmbed(embed);
        DiscordMessage message = await channel.SendMessageAsync(msgBuilder);
        question.messageID = message.Id;
    }
}