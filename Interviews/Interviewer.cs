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
        if (!Database.TryGetInterviewFromTemplate(channel.Parent.Id, channel.Id, out Interview interview))
        {
            return false;
        }

        if (!ConvertReferences(interview, interview.interviewRoot, out string errID))
        {
            DiscordMessage errorMessage = await channel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Description = "Error: Could not start interview, the referenced step id '" + errID + "' does not exist in the interview template step definitions.",
                Color = DiscordColor.Red
            });
            interview.interviewRoot.AddRelatedMessageIDs(errorMessage.Id);
            Database.SaveInterview(interview);
            return false;
        }

        await SendNextMessage(interview, channel, interview.interviewRoot);
        return Database.SaveInterview(interview);
    }

    public static async Task<bool> RestartInterview(DiscordChannel channel)
    {
        if (!await StopInterview(channel))
        {
            Logger.Error("Failed to stop interview in channel '" + channel.Id + "'.");
            return false;
        }

        return await StartInterview(channel);
    }

    public static async Task<bool> StopInterview(DiscordChannel channel)
    {
        if (Database.TryGetInterview(channel.Id, out Interview interview))
        {
            if (Config.deleteMessagesAfterInterviewEnd)
            {
                await DeletePreviousMessages(interview, channel);
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
        if (interaction?.Channel == null || interaction.Message == null)
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
        if (!Database.TryGetInterview(interaction.Channel.Id, out Interview interview))
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Error: There is no active interview in this ticket, ask an admin to check the bot logs if this seems incorrect."))
                .AsEphemeral());
            return;
        }

        // Return if the current question cannot be found in the interview.
        if (!interview.interviewRoot.TryGetCurrentStep(out InterviewStep currentStep))
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
                if (interaction.Data.Resolved.Roles.Any())
                {
                    answer = interaction.Data.Resolved.Roles.First().Value.Mention;
                }
                else if (interaction.Data.Resolved.Users.Any())
                {
                    answer = interaction.Data.Resolved.Users.First().Value.Mention;
                }
                else if (interaction.Data.Resolved.Channels.Any())
                {
                    answer = interaction.Data.Resolved.Channels.First().Value.Mention;
                }
                else if (interaction.Data.Resolved.Messages.Any())
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
            foreach (KeyValuePair<string, ReferencedInterviewStep> reference in currentStep.references)
            {
                // Skip to the first matching step.
                if (Regex.IsMatch(answer, reference.Key))
                {
                    if (TryGetStepFromReference(interview, reference.Value, out InterviewStep referencedStep))
                    {
                        currentStep.steps.Add(reference.Key, referencedStep);
                        await HandleAnswer(answer, referencedStep, interview, currentStep, interaction.Channel);
                    }
                    currentStep.references.Remove(reference.Key);
                    return;
                }
            }

            foreach (KeyValuePair<string, InterviewStep> step in currentStep.steps)
            {
                // Skip to the first matching step.
                if (Regex.IsMatch(answer, step.Key))
                {
                    await HandleAnswer(answer, step.Value, interview, currentStep, interaction.Channel);
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
            Database.SaveInterview(interview);
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
            await HandleAnswer(stepString, nextStep, interview, currentStep, interaction.Channel);
        }
    }

    private static bool TryGetStepFromReference(Interview interview, ReferencedInterviewStep reference, out InterviewStep step)
    {
        foreach (KeyValuePair<string, InterviewStep> definition in interview.definitions)
        {
            if (reference.id == definition.Key)
            {
                step = definition.Value;
                step.buttonStyle = reference.buttonStyle;
                step.selectorDescription = reference.selectorDescription;
                if (step.messageType != MessageType.ERROR)
                {
                    step.afterReferenceStep = reference.afterReferenceStep;
                }
                return true;
            }
        }

        step = null;
        return false;
    }

    public static async Task ProcessResponseMessage(DiscordMessage answerMessage)
    {
        // Either the message or the referenced message is null.
        if (answerMessage.Channel == null || answerMessage.ReferencedMessage?.Channel == null)
        {
            return;
        }

        // The channel does not have an active interview.
        if (!Database.TryGetInterview(answerMessage.ReferencedMessage.Channel.Id,
                                      out Interview interview))
        {
            return;
        }

        if (!interview.interviewRoot.TryGetCurrentStep(out InterviewStep currentStep))
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
            Database.SaveInterview(interview);
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
            Database.SaveInterview(interview);
            return;
        }

        foreach ((string stepPattern, InterviewStep nextStep) in currentStep.steps)
        {
            // Skip to the first matching step.
            if (!Regex.IsMatch(answerMessage.Content, stepPattern))
            {
                continue;
            }

            await HandleAnswer(answerMessage.Content, nextStep, interview, currentStep, answerMessage.Channel, answerMessage);
            return;
        }

        Logger.Error("The interview for channel " + answerMessage.Channel.Id + " reached a step of type " + currentStep.messageType + " which has no valid next step. Their message was:\n" + answerMessage.Content);
        DiscordMessage errorMessage =await answerMessage.RespondAsync(new DiscordEmbedBuilder
        {
            Description = "Error: Could not determine the next question based on your answer. Check your response and ask an admin to check the bot logs if this seems incorrect.",
            Color = DiscordColor.Red
        });
        currentStep.AddRelatedMessageIDs(answerMessage.Id, errorMessage.Id);
        Database.SaveInterview(interview);
    }

    private static async Task HandleAnswer(string answer,
                                           InterviewStep nextStep,
                                           Interview interview,
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
                if (!ConvertReferences(interview, nextStep, out string errID))
                {
                    if (answerMessage != null)
                    {
                        DiscordMessage errorMessage = await answerMessage.RespondAsync(new DiscordEmbedBuilder
                        {
                            Description = "Error: The referenced step id '" + errID + "' does not exist in the step definitions.",
                            Color = DiscordColor.Red
                        });
                        nextStep.AddRelatedMessageIDs(answerMessage.Id, errorMessage.Id);
                        previousStep.answer = null;
                        previousStep.answerID = 0;
                        Database.SaveInterview(interview);
                    }
                    return;
                }

                await SendNextMessage(interview, channel, nextStep);
                Database.SaveInterview(interview);
                break;
            case MessageType.INTERVIEW_END:
                DiscordEmbedBuilder endEmbed = new()
                {
                    Color = Utilities.StringToColor(nextStep.color),
                    Title = nextStep.heading,
                    Description = nextStep.message,
                };

                if (nextStep.addSummary ?? false)
                {
                    AddSummary(interview, ref endEmbed);
                }

                await channel.SendMessageAsync(endEmbed);

                if (Config.deleteMessagesAfterInterviewEnd)
                {
                    await DeletePreviousMessages(interview, channel);
                }

                if (!Database.TryDeleteInterview(channel.Id))
                {
                    Logger.Error("Could not delete interview from database. Channel ID: " + channel.Id);
                }
                return;
            case MessageType.REFERENCE_END:
                if (interview.interviewRoot.TryGetTakenSteps(out List<InterviewStep> previousSteps))
                {
                    foreach (InterviewStep step in previousSteps)
                    {
                        if (step.afterReferenceStep != null)
                        {
                            // If the referenced step is also a reference end, skip it and try to find another.
                            if (step.afterReferenceStep.messageType == MessageType.REFERENCE_END)
                            {
                                step.afterReferenceStep = null;
                            }
                            else
                            {
                                nextStep = step.afterReferenceStep;
                                step.afterReferenceStep = null;

                                previousStep.steps.Clear();
                                previousStep.steps.Add(answer, nextStep);
                                await HandleAnswer(answer, nextStep, interview, previousStep, channel, answerMessage);
                                return;
                            }
                        }
                    }
                }

                DiscordEmbedBuilder error = new()
                {
                    Color = DiscordColor.Red,
                    Description = "An error occured while trying to find the next interview step."
                };

                if (answerMessage == null)
                {
                    DiscordMessage errorMessage = await channel.SendMessageAsync(error);
                    previousStep.AddRelatedMessageIDs(errorMessage.Id);
                }
                else
                {
                    DiscordMessage errorMessage = await answerMessage.RespondAsync(error);
                    previousStep.AddRelatedMessageIDs(errorMessage.Id, answerMessage.Id);
                }

                Database.SaveInterview(interview);

                Logger.Error("Could not find a step to return to after a reference step in channel " + channel.Id);
                return;
            case MessageType.ERROR:
            default:
                DiscordEmbedBuilder errorEmbed = new()
                {
                    Color = Utilities.StringToColor(nextStep.color),
                    Title = nextStep.heading,
                    Description = nextStep.message
                };

                if (nextStep.addSummary ?? false)
                {
                    AddSummary(interview, ref errorEmbed);
                }

                if (answerMessage == null)
                {
                    DiscordMessage errorMessage = await channel.SendMessageAsync(errorEmbed);
                    previousStep.AddRelatedMessageIDs(errorMessage.Id);
                }
                else
                {
                    DiscordMessage errorMessage = await answerMessage.RespondAsync(errorEmbed);
                    previousStep.AddRelatedMessageIDs(errorMessage.Id, answerMessage.Id);
                }

                Database.SaveInterview(interview);
                return;
        }
    }

    private static void AddSummary(Interview interview, ref DiscordEmbedBuilder embed)
    {
        OrderedDictionary summaryFields = new();
        interview.interviewRoot.GetSummary(ref summaryFields);
        foreach (DictionaryEntry entry in summaryFields)
        {
            embed.AddField((string)entry.Key, (string)entry.Value ?? "-");
        }
    }

    private static bool ConvertReferences(Interview interview, InterviewStep step, out string errorID)
    {
        foreach ((string stepPattern, ReferencedInterviewStep reference) in step.references)
        {
            if (!reference.TryGetReferencedStep(interview.definitions, out InterviewStep referencedStep))
            {
                errorID = reference.id;
                return false;
            }

            step.steps.Add(stepPattern, referencedStep);
        }
        step.references.Clear();
        errorID = "";
        return true;
    }

    private static async Task DeletePreviousMessages(Interview interview, DiscordChannel channel)
    {
        List<ulong> previousMessages = [];
        interview.interviewRoot.GetMessageIDs(ref previousMessages);

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

    private static async Task SendNextMessage(Interview interview, DiscordChannel channel, InterviewStep step)
    {
        DiscordMessageBuilder msgBuilder = new();
        DiscordEmbedBuilder embed = new()
        {
            Color = Utilities.StringToColor(step.color),
            Title = step.heading,
            Description = step.message
        };

        if (step.addSummary ?? false)
        {
            AddSummary(interview, ref embed);
        }

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

                    selectionComponents.Add(new DiscordSelectComponent("supportboi_interviewselector " + selectionBoxes,
                                              string.IsNullOrWhiteSpace(step.selectorPlaceholder) ? "Select an option..." : step.selectorPlaceholder, categoryOptions));
                }

                msgBuilder.AddComponents(selectionComponents);
                break;
            case MessageType.ROLE_SELECTOR:
                msgBuilder.AddComponents(new DiscordRoleSelectComponent("supportboi_interviewroleselector",
                                           string.IsNullOrWhiteSpace(step.selectorPlaceholder) ? "Select a role..." : step.selectorPlaceholder));
                break;
            case MessageType.USER_SELECTOR:
                msgBuilder.AddComponents(new DiscordUserSelectComponent("supportboi_interviewuserselector",
                                           string.IsNullOrWhiteSpace(step.selectorPlaceholder) ? "Select a user..." : step.selectorPlaceholder));
                break;
            case MessageType.CHANNEL_SELECTOR:
                msgBuilder.AddComponents(new DiscordChannelSelectComponent("supportboi_interviewchannelselector",
                                           string.IsNullOrWhiteSpace(step.selectorPlaceholder) ? "Select a channel..." : step.selectorPlaceholder));
                break;
            case MessageType.MENTIONABLE_SELECTOR:
                msgBuilder.AddComponents(new DiscordMentionableSelectComponent("supportboi_interviewmentionableselector",
                                           string.IsNullOrWhiteSpace(step.selectorPlaceholder) ? "Select a user or role..." : step.selectorPlaceholder));
                break;
            case MessageType.TEXT_INPUT:
                embed.WithFooter("Reply to this message with your answer. You cannot include images or files.");
                break;
            case MessageType.INTERVIEW_END:
            case MessageType.ERROR:
            default:
                break;
        }

        msgBuilder.AddEmbed(embed);
        DiscordMessage message = await channel.SendMessageAsync(msgBuilder);
        step.messageID = message.Id;
    }
}