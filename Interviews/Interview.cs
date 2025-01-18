using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SupportBoi.Interviews;

public enum MessageType
{
    // TODO: Support multiselector as separate type
    ERROR,
    END_WITH_SUMMARY,
    END_WITHOUT_SUMMARY,
    BUTTONS,
    TEXT_SELECTOR,
    USER_SELECTOR,
    ROLE_SELECTOR,
    MENTIONABLE_SELECTOR, // User or role
    CHANNEL_SELECTOR,
    TEXT_INPUT,
    REFERENCE_END
}

public enum ButtonType
{
    PRIMARY,
    SECONDARY,
    SUCCESS,
    DANGER
}

public class ReferencedInterviewStep
{
    [JsonProperty("id")]
    public string id;

    // If this step is on a button, give it this style.
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty("button-style")]
    public ButtonType? buttonStyle;

    // If this step is in a selector, give it this description.
    [JsonProperty("selector-description")]
    public string selectorDescription;

    // Runs at the end of the reference
    [JsonProperty("after-reference-step")]
    public InterviewStep afterReferenceStep;

    public DiscordButtonStyle GetButtonStyle()
    {
        return InterviewStep.GetButtonStyle(buttonStyle);
    }

    public bool TryGetReferencedStep(Interview interview, out InterviewStep step)
    {
        if (!interview.definitions.TryGetValue(id, out step))
        {
            Logger.Error("Could not find referenced step '" + id + "' in interview for channel '" + interview.channelID + "'");
            return false;
        }

        step.buttonStyle = buttonStyle;
        step.selectorDescription = selectorDescription;
        step.afterReferenceStep = afterReferenceStep;

        return true;
    }
}

// A tree of steps representing an interview.
// The tree is generated by the config file when a new ticket is opened or the restart interview command is used.
// Additional components not specified in the config file are populated as the interview progresses.
// The entire interview tree is serialized and stored in the database to record responses as they are made.
public class InterviewStep
{
    // Title of the message embed.
    [JsonProperty("heading")]
    public string heading;

    // Message contents sent to the user.
    [JsonProperty("message")]
    public string message;

    // The type of message.
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty("message-type")]
    public MessageType messageType;

    // Colour of the message embed.
    [JsonProperty("color")]
    public string color = "CYAN";

    // Used as label for this answer in the post-interview summary.
    [JsonProperty("summary-field")]
    public string summaryField;

    // If this step is on a button, give it this style.
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty("button-style")]
    public ButtonType? buttonStyle;

    // If this step is a selector, give it this placeholder.
    [JsonProperty("selector-placeholder")]
    public string selectorPlaceholder;

    // If this step is in a selector, give it this description.
    [JsonProperty("selector-description")]
    public string selectorDescription;

    // The maximum length of a text input.
    [JsonProperty("max-length")]
    public int? maxLength;

    // The minimum length of a text input.
    [JsonProperty("min-length")]
    public int? minLength;

    // References to steps defined elsewhere in the template
    [JsonProperty("step-references")]
    public Dictionary<string, ReferencedInterviewStep> references = new();

    // Possible questions to ask next, an error message, or the end of the interview.
    [JsonProperty("steps")]
    public Dictionary<string, InterviewStep> steps = new();

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

    // This is only set when the user gets to a referenced step
    [JsonProperty("after-reference-step")]
    public InterviewStep afterReferenceStep = null;

    public bool TryGetCurrentStep(out InterviewStep currentStep)
    {
        bool result = TryGetTakenSteps(out List<InterviewStep> previousSteps);
        currentStep = previousSteps.FirstOrDefault();
        return result;
    }

    public bool TryGetTakenSteps(out List<InterviewStep> previousSteps)
    {
        // This object has not been initialized, we have checked too deep.
        if (messageID == 0)
        {
            previousSteps = null;
            return false;
        }

        // Check children.
        foreach (KeyValuePair<string,InterviewStep> childStep in steps)
        {
            // This child either is the one we are looking for or contains the one we are looking for.
            if (childStep.Value.TryGetTakenSteps(out previousSteps))
            {
                previousSteps.Add(this);
                return true;
            }
        }

        // This object is the deepest object with a message ID set, meaning it is the latest asked question.
        previousSteps = new List<InterviewStep> { this };
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
            // TODO: Add option to merge answers
            summary[summaryField] = answer;
        }

        // This will always contain exactly one or zero children.
        foreach (KeyValuePair<string, InterviewStep> step in steps)
        {
            step.Value.GetSummary(ref summary);
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
        foreach (KeyValuePair<string, InterviewStep> step in steps)
        {
            step.Value.GetMessageIDs(ref messageIDs);
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

    public void Validate(ref List<string> errors,
                         ref List<string> warnings,
                         string stepID,
                         int summaryFieldCount = 0,
                         int summaryMaxLength = 0,
                         InterviewStep parent = null)
    {
        if (!string.IsNullOrWhiteSpace(summaryField))
        {
            ++summaryFieldCount;
            summaryMaxLength += summaryField.Length;
            switch (messageType)
            {
                case MessageType.BUTTONS:
                case MessageType.TEXT_SELECTOR:
                    // Get the longest button/selector text
                    if (steps.Count > 0)
                    {
                        summaryMaxLength += steps.Max(kv => kv.Key.Length);
                    }
                    break;
                case MessageType.USER_SELECTOR:
                case MessageType.ROLE_SELECTOR:
                case MessageType.MENTIONABLE_SELECTOR:
                case MessageType.CHANNEL_SELECTOR:
                    // Approximate length of a mention
                    summaryMaxLength += 23;
                    break;
                case MessageType.TEXT_INPUT:
                    summaryMaxLength += Math.Min(maxLength ?? 1024, 1024);
                    break;
                case MessageType.END_WITH_SUMMARY:
                case MessageType.END_WITHOUT_SUMMARY:
                case MessageType.ERROR:
                default:
                    break;
            }
        }

        if (messageType is MessageType.ERROR or MessageType.END_WITH_SUMMARY or MessageType.END_WITHOUT_SUMMARY)
        {
            if (steps.Count > 0 || references.Count > 0)
            {
                warnings.Add("Steps of the type '" + messageType + "' cannot have child steps.\n\n" + stepID + ".message-type");
            }

            if (!string.IsNullOrWhiteSpace(summaryField))
            {
                warnings.Add("Steps of the type '" + messageType + "' cannot have summary field names.\n\n" + stepID + ".summary-field");
            }
        }
        else if (steps.Count == 0 && references.Count == 0)
        {
            errors.Add("Steps of the type '" + messageType + "' must have at least one child step.\n\n" + stepID + ".message-type");
        }

        if (messageType is MessageType.END_WITH_SUMMARY)
        {
            summaryMaxLength += message?.Length ?? 0;
            summaryMaxLength += heading?.Length ?? 0;
            if (summaryFieldCount > 25)
            {
                errors.Add("A summary cannot contain more than 25 fields, but you have " + summaryFieldCount + " fields in this branch.\n\n" + stepID);
            }
            else if (summaryMaxLength >= 6000)
            {
                warnings.Add("A summary cannot contain more than 6000 characters, but this branch may reach " + summaryMaxLength + " characters.\n" +
                             "Use the \"max-length\" parameter to limit text input field lengths, or shorten other parts of the summary message.\n\n" + stepID);
            }
        }

        if (parent?.messageType is not MessageType.BUTTONS && buttonStyle != null)
        {
            warnings.Add("Button styles have no effect on child steps of a '" + parent?.messageType + "' step.\n\n" + stepID + ".button-style");
        }

        if (parent?.messageType is not MessageType.TEXT_SELECTOR && selectorDescription != null)
        {
            warnings.Add("Selector descriptions have no effect on child steps of a '" + parent?.messageType + "' step.\n\n" + stepID + ".selector-description");
        }

        if (messageType is not MessageType.TEXT_SELECTOR && selectorPlaceholder != null)
        {
            warnings.Add("Selector placeholders have no effect on steps of the type '" + messageType + "'.\n\n" + stepID + ".selector-placeholder");
        }

        if (messageType is not MessageType.TEXT_INPUT && maxLength != null)
        {
            warnings.Add("Max length has no effect on steps of the type '" + messageType + "'.\n\n" + stepID + ".max-length");
        }

        if (messageType is not MessageType.TEXT_INPUT && minLength != null)
        {
            warnings.Add("Min length has no effect on steps of the type '" + messageType + "'.\n\n" + stepID + ".min-length");
        }

        foreach (KeyValuePair<string,InterviewStep> step in steps)
        {
            // The JSON schema error messages use this format for the JSON path, so we use it here too.
            string nextStepID = stepID;
            nextStepID += step.Key.ContainsAny('.', ' ', '[', ']', '(', ')', '/', '\\')
                ? ".steps['" + step.Key + "']"
                : ".steps." + step.Key;

            step.Value.Validate(ref errors, ref warnings, nextStepID, summaryFieldCount, summaryMaxLength, this);
        }
    }

    public DiscordButtonStyle GetButtonStyle()
    {
        return GetButtonStyle(buttonStyle);
    }

    public static DiscordButtonStyle GetButtonStyle(ButtonType? buttonStyle)
    {
        return buttonStyle switch
        {
            ButtonType.PRIMARY   => DiscordButtonStyle.Primary,
            ButtonType.SECONDARY => DiscordButtonStyle.Secondary,
            ButtonType.SUCCESS   => DiscordButtonStyle.Success,
            ButtonType.DANGER    => DiscordButtonStyle.Danger,
            _                    => DiscordButtonStyle.Secondary
        };
    }

    public class StripInternalPropertiesResolver : DefaultContractResolver
    {
        private static readonly HashSet<string> ignoreProps =
        [
            "message-id",
            "answer",
            "answer-id",
            "related-message-ids"
        ];

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (ignoreProps.Contains(property.PropertyName))
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
}

public class Interview(ulong channelID, InterviewStep interviewRoot, Dictionary<string, InterviewStep> definitions)
{
    public ulong channelID = channelID;
    public InterviewStep interviewRoot = interviewRoot;
    public Dictionary<string, InterviewStep> definitions = definitions;
}

public class Template(ulong categoryID, InterviewStep interview, Dictionary<string, InterviewStep> definitions)
{
    [JsonProperty("category-id", Required = Required.Always)]
    public ulong categoryID = categoryID;

    [JsonProperty("interview", Required = Required.Always)]
    public InterviewStep interview = interview;

    [JsonProperty("definitions", Required = Required.Default)]
    public Dictionary<string, InterviewStep> definitions = definitions;
}