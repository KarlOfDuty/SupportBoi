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
        if (!Database.TryGetInterviewTemplate(channel.Parent.Id, out InterviewStep template))
        {
            return false;
        }

        await SendNextMessage(channel, template);
        return Database.SaveInterview(channel.Id, template);
    }

    public static async Task<bool> RestartInterview(DiscordChannel channel)
    {
        if (Database.TryGetInterview(channel.Id, out InterviewStep interviewRoot))
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
        if (Database.TryGetInterview(channel.Id, out InterviewStep interviewRoot))
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
        if (!Database.TryGetInterview(interaction.Channel.Id, out InterviewStep interviewRoot))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Error: There is no active interview in this ticket, ask an admin to check the bot logs if this seems incorrect."))
                .AsEphemeral());
            return;
        }

        // Return if the current question cannot be found in the interview.
        if (!interviewRoot.TryGetCurrentStep(out InterviewStep currentStep))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Error: Something seems to have broken in this interview, you may want to restart it."))
                .AsEphemeral());
            Logger.Error("The interview for channel " + interaction.Channel.Id + " exists but does not have a message ID set for it's root interview step");
            return;
        }

        // Check if this button/selector is for an older question.
        if (interaction.Message.Id != currentStep.messageID)
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
            foreach (KeyValuePair<string, InterviewStep> step in currentStep.steps)
            {
                // Skip to the first matching step.
                if (Regex.IsMatch(answer, step.Key))
                {
                    await HandleAnswer(answer, step.Value, interviewRoot, currentStep, interaction.Channel);
                    return;
                }
            }

            Logger.Error("The interview for channel " + interaction.Channel.Id + " reached a step of type " + currentStep.messageType + " which has no valid next step. Their selection was:\n" + answer);
            DiscordMessage followupMessage = await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Error: Could not determine the next question based on your answer. Check your response and ask an admin to check the bot logs if this seems incorrect."
            }).AsEphemeral());
            currentStep.AddRelatedMessageIDs(followupMessage.Id);
            Database.SaveInterview(interaction.Channel.Id, interviewRoot);
        }
        else
        {
            if (!int.TryParse(componentID, out int stepIndex))
            {
                Logger.Error("Invalid interview button/selector index: " + componentID);
                return;
            }

            if (stepIndex >= currentStep.steps.Count || stepIndex < 0)
            {
                Logger.Error("Invalid interview button/selector index: " + stepIndex);
                return;
            }

            (string stepString, InterviewStep nextStep) = currentStep.steps.ElementAt(stepIndex);
            await HandleAnswer(stepString, nextStep, interviewRoot, currentStep, interaction.Channel);
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
        if (!Database.TryGetInterview(answerMessage.ReferencedMessage.Channel.Id, out InterviewStep interviewRoot))
        {
            return;
        }

        if (!interviewRoot.TryGetCurrentStep(out InterviewStep currentStep))
        {
            return;
        }

        // The user responded to something other than the latest interview question.
        if (answerMessage.ReferencedMessage.Id != currentStep.messageID)
        {
            return;
        }

        // The user responded to a question which does not take a text response.
        if (currentStep.messageType != MessageType.TEXT_INPUT)
        {
            return;
        }

        // The length requirement is less than 1024 characters, and must be less than the configurable limit if it is set.
        int maxLength = Math.Min(currentStep.maxLength ?? 1024, 1024);

        if (answerMessage.Content.Length > maxLength)
        {
            DiscordMessage lengthMessage = await answerMessage.RespondAsync(new DiscordEmbedBuilder
            {
                Description = "Error: Your answer cannot be more than " + maxLength + " characters (" + answerMessage.Content.Length + "/" + maxLength + ").",
                Color = DiscordColor.Red
            });
            currentStep.AddRelatedMessageIDs(answerMessage.Id, lengthMessage.Id);
            Database.SaveInterview(answerMessage.Channel.Id, interviewRoot);
            return;
        }

        if (answerMessage.Content.Length < (currentStep.minLength ?? 0))
        {
            DiscordMessage lengthMessage = await answerMessage.RespondAsync(new DiscordEmbedBuilder
            {
                Description = "Error: Your answer must be at least " + currentStep.minLength + " characters (" + answerMessage.Content.Length + "/" + currentStep.minLength + ").",
                Color = DiscordColor.Red
            });
            currentStep.AddRelatedMessageIDs(answerMessage.Id, lengthMessage.Id);
            Database.SaveInterview(answerMessage.Channel.Id, interviewRoot);
            return;
        }

        foreach ((string stepPattern, InterviewStep nextStep) in currentStep.steps)
        {
            // Skip to the first matching step.
            if (!Regex.IsMatch(answerMessage.Content, stepPattern))
            {
                continue;
            }

            await HandleAnswer(answerMessage.Content, nextStep, interviewRoot, currentStep, answerMessage.Channel, answerMessage);
            return;
        }

        Logger.Error("The interview for channel " + answerMessage.Channel.Id + " reached a step of type " + currentStep.messageType + " which has no valid next step. Their message was:\n" + answerMessage.Content);
        DiscordMessage errorMessage =await answerMessage.RespondAsync(new DiscordEmbedBuilder
        {
            Description = "Error: Could not determine the next question based on your answer. Check your response and ask an admin to check the bot logs if this seems incorrect.",
            Color = DiscordColor.Red
        });
        currentStep.AddRelatedMessageIDs(answerMessage.Id, errorMessage.Id);
        Database.SaveInterview(answerMessage.Channel.Id, interviewRoot);
    }

    private static async Task HandleAnswer(string answer,
                                           InterviewStep nextStep,
                                           InterviewStep interviewRoot,
                                           InterviewStep previousStep,
                                           DiscordChannel channel,
                                           DiscordMessage answerMessage = null)
    {
        // The error message type should not alter anything about the interview.
        if (nextStep.messageType != MessageType.ERROR)
        {
            previousStep.answer = answer;

            // There is no message ID if the step is not a text input.
            previousStep.answerID = answerMessage == null ? 0 : answerMessage.Id;
        }

        // Create next step, or finish the interview.
        switch (nextStep.messageType)
        {
            case MessageType.TEXT_INPUT:
            case MessageType.BUTTONS:
            case MessageType.TEXT_SELECTOR:
            case MessageType.ROLE_SELECTOR:
            case MessageType.USER_SELECTOR:
            case MessageType.CHANNEL_SELECTOR:
            case MessageType.MENTIONABLE_SELECTOR:
                await SendNextMessage(channel, nextStep);
                Database.SaveInterview(channel.Id, interviewRoot);
                break;
            case MessageType.END_WITH_SUMMARY:
                OrderedDictionary summaryFields = new OrderedDictionary();
                interviewRoot.GetSummary(ref summaryFields);

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                {
                    Color = Utilities.StringToColor(nextStep.color),
                    Title = nextStep.heading,
                    Description = nextStep.message,
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
            case MessageType.END_WITHOUT_SUMMARY:
                await channel.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Color = Utilities.StringToColor(nextStep.color),
                    Title = nextStep.heading,
                    Description = nextStep.message
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
            case MessageType.ERROR:
            default:
                if (answerMessage == null)
                {
                    DiscordMessage errorMessage = await channel.SendMessageAsync(new DiscordEmbedBuilder()
                    {
                        Color = Utilities.StringToColor(nextStep.color),
                        Title = nextStep.heading,
                        Description = nextStep.message
                    });
                    previousStep.AddRelatedMessageIDs(errorMessage.Id);
                }
                else
                {
                    DiscordMessageBuilder errorMessageBuilder = new DiscordMessageBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                        {
                            Color = Utilities.StringToColor(nextStep.color),
                            Title = nextStep.heading,
                            Description = nextStep.message
                        }).WithReply(answerMessage.Id);
                    DiscordMessage errorMessage = await answerMessage.RespondAsync(errorMessageBuilder);
                    previousStep.AddRelatedMessageIDs(errorMessage.Id, answerMessage.Id);
                }

                Database.SaveInterview(channel.Id, interviewRoot);
                break;
        }
    }

    private static async Task DeletePreviousMessages(InterviewStep interviewRoot, DiscordChannel channel)
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

    private static async Task SendNextMessage(DiscordChannel channel, InterviewStep step)
    {
        DiscordMessageBuilder msgBuilder = new();
        DiscordEmbedBuilder embed = new()
        {
            Color = Utilities.StringToColor(step.color),
            Title = step.heading,
            Description = step.message
        };

        switch (step.messageType)
        {
            case MessageType.BUTTONS:
                int nrOfButtons = 0;
                for (int nrOfButtonRows = 0; nrOfButtonRows < 5 && nrOfButtons < step.steps.Count; nrOfButtonRows++)
                {
                    List<DiscordButtonComponent> buttonRow = [];
                    for (; nrOfButtons < 5 * (nrOfButtonRows + 1) && nrOfButtons < step.steps.Count; nrOfButtons++)
                    {
                        (string stepPattern, InterviewStep nextStep) = step.steps.ToArray()[nrOfButtons];
                        buttonRow.Add(new DiscordButtonComponent(nextStep.GetButtonStyle(), "supportboi_interviewbutton " + nrOfButtons, stepPattern));
                    }
                    msgBuilder.AddComponents(buttonRow);
                }
                break;
            case MessageType.TEXT_SELECTOR:
                List<DiscordSelectComponent> selectionComponents = [];

                int selectionOptions = 0;
                for (int selectionBoxes = 0; selectionBoxes < 5 && selectionOptions < step.steps.Count; selectionBoxes++)
                {
                    List<DiscordSelectComponentOption> categoryOptions = [];
                    for (; selectionOptions < 25 * (selectionBoxes + 1) && selectionOptions < step.steps.Count; selectionOptions++)
                    {
                        (string stepPattern, InterviewStep nextStep) = step.steps.ToArray()[selectionOptions];
                        categoryOptions.Add(new DiscordSelectComponentOption(stepPattern, selectionOptions.ToString(), nextStep.selectorDescription));
                    }

                    selectionComponents.Add(new DiscordSelectComponent("supportboi_interviewselector " + selectionBoxes, string.IsNullOrWhiteSpace(step.selectorPlaceholder)
                                                                                                                           ? "Select an option..." : step.selectorPlaceholder, categoryOptions));
                }

                msgBuilder.AddComponents(selectionComponents);
                break;
            case MessageType.ROLE_SELECTOR:
                msgBuilder.AddComponents(new DiscordRoleSelectComponent("supportboi_interviewroleselector", string.IsNullOrWhiteSpace(step.selectorPlaceholder)
                                                                                                              ? "Select a role..." : step.selectorPlaceholder));
                break;
            case MessageType.USER_SELECTOR:
                msgBuilder.AddComponents(new DiscordUserSelectComponent("supportboi_interviewuserselector", string.IsNullOrWhiteSpace(step.selectorPlaceholder)
                                                                                                              ? "Select a user..." : step.selectorPlaceholder));
                break;
            case MessageType.CHANNEL_SELECTOR:
                msgBuilder.AddComponents(new DiscordChannelSelectComponent("supportboi_interviewchannelselector", string.IsNullOrWhiteSpace(step.selectorPlaceholder)
                                                                                                                    ? "Select a channel..." : step.selectorPlaceholder));
                break;
            case MessageType.MENTIONABLE_SELECTOR:
                msgBuilder.AddComponents(new DiscordMentionableSelectComponent("supportboi_interviewmentionableselector", string.IsNullOrWhiteSpace(step.selectorPlaceholder)
                                                                                                                            ? "Select a user or role..." : step.selectorPlaceholder));
                break;
            case MessageType.TEXT_INPUT:
                embed.WithFooter("Reply to this message with your answer. You cannot include images or files.");
                break;
            case MessageType.END_WITH_SUMMARY:
            case MessageType.END_WITHOUT_SUMMARY:
            case MessageType.ERROR:
            default:
                break;
        }

        msgBuilder.AddEmbed(embed);
        DiscordMessage message = await channel.SendMessageAsync(msgBuilder);
        step.messageID = message.Id;
    }
}