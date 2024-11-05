using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SupportBoi;

public static class Interviewer
{
    // TODO: Validate that the different types have the appropriate amount of subpaths
    public enum QuestionType
    {
        // Support multiselector as separate type, with only one subpath supported
        ERROR,
        END_WITH_SUMMARY,
        END_WITHOUT_SUMMARY,
        BUTTONS,
        TEXT_SELECTOR,
        USER_SELECTOR,
        ROLE_SELECTOR,
        MENTIONABLE_SELECTOR, // User or role
        CHANNEL_SELECTOR,
        TEXT_INPUT
    }

    public enum ButtonType
    {
        // Secondary first to make it the default
        SECONDARY,
        PRIMARY,
        SUCCESS,
        DANGER
    }

    // A tree of questions representing an interview.
    // The tree is generated by the config file when a new ticket is opened or the restart interview command is used.
    // Additional components not specified in the config file are populated as the interview progresses.
    // The entire interview tree is serialized and stored in the database in order to record responses as they are made.
    public class InterviewQuestion
    {
        // Title of the message embed.
        [JsonProperty("title")]
        public string title;

        // Message contents sent to the user.
        [JsonProperty("message")]
        public string message;

        // The type of question.
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("type")]
        public QuestionType type;

        // Colour of the message embed.
        [JsonProperty("color")]
        public string color;

        // Used as label for this question in the post-interview summary.
        [JsonProperty("summary-field")]
        public string summaryField;

        // If this question is on a button, give it this style.
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("button-style")]
        public ButtonType buttonStyle;

        // If this question is on a selector, give it this placeholder.
        [JsonProperty("selector-placeholder")]
        public string selectorPlaceholder;

        // The maximum length of a text input.
        [JsonProperty("max-length")]
        public int maxLength;

        // The minimum length of a text input.
        [JsonProperty("min-length", Required = Required.Default)]
        public int minLength;

        // Possible questions to ask next, an error message, or the end of the interview.
        [JsonProperty("paths")]
        public Dictionary<string, InterviewQuestion> paths;

        // ////////////////////////////////////////////////////////////////////////////
        // The following parameters are populated by the bot, not the json template. //
        // ////////////////////////////////////////////////////////////////////////////

        // The ID of this message where the bot asked this question.
        [JsonProperty("message-id")]
        public ulong messageID;

        // The contents of the user's answer.
        [JsonProperty("answer")]
        public string answer;

        // The ID of the user's answer message if this is a TEXT_INPUT type.
        [JsonProperty("answer-id")]
        public ulong answerID;

        // Any extra messages generated by the bot that should be removed when the interview ends.
        [JsonProperty("related-message-ids")]
        public List<ulong> relatedMessageIDs;

        public bool TryGetCurrentQuestion(out InterviewQuestion question)
        {
            // This object has not been initialized, we have checked too deep.
            if (messageID == 0)
            {
                question = null;
                return false;
            }

            // Check children.
            foreach (KeyValuePair<string,InterviewQuestion> path in paths)
            {
                // This child either is the one we are looking for or contains the one we are looking for.
                if (path.Value.TryGetCurrentQuestion(out question))
                {
                    return true;
                }
            }

            // This object is the deepest object with a message ID set, meaning it is the latest asked question.
            question = this;
            return true;
        }

        public void GetSummary(ref OrderedDictionary summary)
        {
            if (messageID == 0)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(summaryField))
            {
                summary.Add(summaryField, answer);
            }

            // This will always contain exactly one or zero children.
            foreach (KeyValuePair<string, InterviewQuestion> path in paths)
            {
                path.Value.GetSummary(ref summary);
            }
        }

        public void GetMessageIDs(ref List<ulong> messageIDs)
        {
            if (messageID != 0)
            {
                messageIDs.Add(messageID);
            }

            if (answerID != 0)
            {
                messageIDs.Add(answerID);
            }

            if (relatedMessageIDs != null)
            {
                messageIDs.AddRange(relatedMessageIDs);
            }

            // This will always contain exactly one or zero children.
            foreach (KeyValuePair<string, InterviewQuestion> path in paths)
            {
                path.Value.GetMessageIDs(ref messageIDs);
            }
        }

        public void AddRelatedMessageIDs(params ulong[] messageIDs)
        {
            if (relatedMessageIDs == null)
            {
                relatedMessageIDs = messageIDs.ToList();
            }
            else
            {
                relatedMessageIDs.AddRange(messageIDs);
            }
        }

        public DiscordButtonStyle GetButtonStyle()
        {
            return buttonStyle switch
            {
                ButtonType.PRIMARY   => DiscordButtonStyle.Primary,
                ButtonType.SECONDARY => DiscordButtonStyle.Secondary,
                ButtonType.SUCCESS   => DiscordButtonStyle.Success,
                ButtonType.DANGER    => DiscordButtonStyle.Danger,
                _                    => DiscordButtonStyle.Primary
            };
        }
    }

    // This class is identical to the one above and just exists as a hack to get JSON validation when
    // new entries are entered but not when read from database in order to be more lenient with old interviews.
    // I might do this in a more proper way at some point.
    public class ValidatedInterviewQuestion
    {
        [JsonProperty("title", Required = Required.Default)]
        public string title;

        [JsonProperty("message", Required = Required.Always)]
        public string message;

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("type", Required = Required.Always)]
        public QuestionType type;

        [JsonProperty("color", Required = Required.Always)]
        public string color;

        [JsonProperty("summary-field", Required = Required.Default)]
        public string summaryField;

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("button-style", Required = Required.Default)]
        public ButtonType buttonStyle;

        [JsonProperty("selector-placeholder", Required = Required.Default)]
        public string selectorPlaceholder;

        [JsonProperty("max-length", Required = Required.Default)]
        public int maxLength;

        [JsonProperty("min-length", Required = Required.Default)]
        public int minLength;

        [JsonProperty("paths", Required = Required.Always)]
        public Dictionary<string, ValidatedInterviewQuestion> paths;
    }

    private static Dictionary<ulong, InterviewQuestion> activeInterviews = [];

    public static void ReloadInterviews()
    {
        activeInterviews = Database.GetAllInterviews();
    }

    public static async void StartInterview(DiscordChannel channel)
    {
        if (channel.Parent == null)
        {
            return;
        }

        if (!Database.TryGetInterviewTemplates(out Dictionary<ulong, InterviewQuestion> templates))
        {
            await channel.SendMessageAsync(new DiscordEmbedBuilder
            {
                Description = "Attempted to create interview from template, but an error occured when reading it from the database.\n\n" +
                              "Tell a staff member to check the bot log and fix the template.",
                Color = DiscordColor.Red
            });
            return;
        }

        if (templates.TryGetValue(channel.Parent.Id, out InterviewQuestion interview))
        {
            await CreateQuestion(channel, interview);
            Database.SaveInterview(channel.Id, interview);
            activeInterviews = Database.GetAllInterviews();
        }
    }

    public static async Task RestartInterview(SlashCommandContext command)
    {
        if (activeInterviews.TryGetValue(command.Channel.Id, out InterviewQuestion interviewRoot))
        {
            await DeletePreviousMessages(interviewRoot, command.Channel);
            if (!Database.TryDeleteInterview(command.Channel.Id))
            {
                Logger.Error("Could not delete interview from database. Channel ID: " + command.Channel.Id);
            }
            ReloadInterviews();
        }
        StartInterview(command.Channel);
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
        if (!activeInterviews.TryGetValue(interaction.Channel.Id, out InterviewQuestion interviewRoot))
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
            // TODO: Debug this with the new selectors
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
            default:
                throw new ArgumentOutOfRangeException();
        }

        // The different mentionable selectors provide the actual answer, while the others just return the ID.
        if (componentID == "")
        {
            // TODO: Handle multipaths
            if (currentQuestion.paths.Count != 1)
            {
                Logger.Error("The interview for channel " + interaction.Channel.Id + " has a question of type " + currentQuestion.type + " and it must have exactly one subpath.");
                return;
            }

            (string _, InterviewQuestion nextQuestion) = currentQuestion.paths.First();
            await HandleAnswer(answer, nextQuestion, interviewRoot, currentQuestion, interaction.Channel);
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
        if (!activeInterviews.TryGetValue(answerMessage.ReferencedMessage.Channel.Id, out InterviewQuestion interviewRoot))
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
        int maxLength = currentQuestion.maxLength == 0 ? 1024 : Math.Min(currentQuestion.maxLength, 1024);

        if (answerMessage.Content.Length > maxLength)
        {
            DiscordMessage lengthMessage = await answerMessage.RespondAsync(new DiscordEmbedBuilder
            {
                Description = "Error: Your answer cannot be more than " + maxLength + " characters (" + answerMessage.Content.Length + "/" + maxLength + ").",
                Color = DiscordColor.Red
            });
            currentQuestion.AddRelatedMessageIDs(answerMessage.Id, lengthMessage.Id);
            return;
        }

        if (answerMessage.Content.Length < currentQuestion.minLength)
        {
            DiscordMessage lengthMessage = await answerMessage.RespondAsync(new DiscordEmbedBuilder
            {
                Description = "Error: Your answer must be at least " + currentQuestion.minLength + " characters (" + answerMessage.Content.Length + "/" + currentQuestion.minLength + ").",
                Color = DiscordColor.Red
            });
            currentQuestion.AddRelatedMessageIDs(answerMessage.Id, lengthMessage.Id);
            return;
        }

        foreach ((string questionString, InterviewQuestion nextQuestion) in currentQuestion.paths)
        {
            // Skip to the first matching path.
            if (!Regex.IsMatch(answerMessage.Content, questionString)) continue;

            await HandleAnswer(answerMessage.Content, nextQuestion, interviewRoot, currentQuestion, answerMessage.Channel, answerMessage);
            return;
        }

        // TODO: Make message configurable.
        DiscordMessage errorMessage =await answerMessage.RespondAsync(new DiscordEmbedBuilder
        {
            Description = "Error: Could not determine the next question based on your answer.",
            Color = DiscordColor.Red
        });
        currentQuestion.AddRelatedMessageIDs(errorMessage.Id);
    }

    private static async Task HandleAnswer(string answer,
                                           InterviewQuestion nextQuestion,
                                           InterviewQuestion interviewRoot,
                                           InterviewQuestion previousQuestion,
                                           DiscordChannel channel,
                                           DiscordMessage answerMessage = null)
    {
        // The error message type should not alter anything about the interview
        if (nextQuestion.type != QuestionType.ERROR)
        {
            previousQuestion.answer = answer;
            if (answerMessage == null)
            {
                // The answer was provided using a button or selector
                previousQuestion.answerID = 0;
            }
            else
            {
                previousQuestion.answerID = answerMessage.Id;
            }
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

                await DeletePreviousMessages(interviewRoot, channel);
                if (!Database.TryDeleteInterview(channel.Id))
                {
                    Logger.Error("Could not delete interview from database. Channel ID: " + channel.Id);
                }
                ReloadInterviews();
                return;
            case QuestionType.END_WITHOUT_SUMMARY:
                await channel.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Color = Utilities.StringToColor(nextQuestion.color),
                    Title = nextQuestion.title,
                    Description = nextQuestion.message
                });

                await DeletePreviousMessages(interviewRoot, channel);
                if (!Database.TryDeleteInterview(channel.Id))
                {
                    Logger.Error("Could not delete interview from database. Channel ID: " + channel.Id);
                }
                ReloadInterviews();
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
                break;
        }
    }

    private static async Task DeletePreviousMessages(InterviewQuestion interviewRoot, DiscordChannel channel)
    {
        List<ulong> previousMessages = new List<ulong> { };
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
                        categoryOptions.Add(new DiscordSelectComponentOption(question.paths.ToArray()[selectionOptions].Key, selectionOptions.ToString()));
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