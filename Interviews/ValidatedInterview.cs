using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SupportBoi.Interviews;

// This class is identical to the normal interview question and just exists as a hack to get JSON validation when
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
    public ButtonType? buttonStyle;

    [JsonProperty("selector-placeholder", Required = Required.Default)]
    public string selectorPlaceholder;

    [JsonProperty("selector-description", Required = Required.Default)]
    public string selectorDescription;

    [JsonProperty("max-length", Required = Required.Default)]
    public int? maxLength;

    [JsonProperty("min-length", Required = Required.Default)]
    public int? minLength;

    [JsonProperty("paths", Required = Required.Always)]
    public Dictionary<string, ValidatedInterviewQuestion> paths;

    public void Validate(ref List<string> errors, out int summaryCount, out int summaryMaxLength)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            errors.Add("Message cannot be empty.");
        }

        if (type is QuestionType.ERROR or QuestionType.END_WITH_SUMMARY or QuestionType.END_WITHOUT_SUMMARY)
        {
            if (paths.Count > 0)
            {
                errors.Add("'" + type + "' questions cannot have child paths.");
            }
        }
        else if (paths.Count == 0)
        {
            errors.Add("'" + type + "' questions must have at least one child path.");
        }

        List<int> summaryCounts = [];
        Dictionary<string, int> childMaxLengths = new Dictionary<string, int>();
        foreach (KeyValuePair<string,ValidatedInterviewQuestion> path in paths)
        {
            path.Value.Validate(ref errors, out int summaries, out int maxLen);
            summaryCounts.Add(summaries);
            childMaxLengths.Add(path.Key, maxLen);
        }

        summaryCount = summaryCounts.Count == 0 ? 0 : summaryCounts.Max();

        string childPathString = "";
        int childMaxLength = 0;
        if (childMaxLengths.Count != 0)
        {
            (childPathString, childMaxLength) = childMaxLengths.ToArray().MaxBy(x => x.Key.Length + x.Value);
        }

        summaryMaxLength = childMaxLength;

        if (string.IsNullOrWhiteSpace(summaryField))
        {
            ++summaryCount;
        }

        // Only count summaries that end in a summary question.
        if (type == QuestionType.END_WITH_SUMMARY)
        {
            summaryMaxLength = message?.Length ?? 0;
            summaryMaxLength += title?.Length ?? 0;
        }
        // Only add to the total max length if the summary field is not empty. That way we know this branch ends in a summary.
        else if (summaryMaxLength > 0 && !string.IsNullOrEmpty(summaryField))
        {
            summaryMaxLength += summaryField.Length;
            switch (type)
            {
                case QuestionType.BUTTONS:
                case QuestionType.TEXT_SELECTOR:
                    summaryMaxLength += childPathString.Length;
                    break;
                case QuestionType.USER_SELECTOR:
                case QuestionType.ROLE_SELECTOR:
                case QuestionType.MENTIONABLE_SELECTOR:
                case QuestionType.CHANNEL_SELECTOR:
                    // Approximate length of a mention
                    summaryMaxLength += 23;
                    break;
                case QuestionType.TEXT_INPUT:
                    summaryMaxLength += Math.Min(maxLength ?? 1024, 1024);
                    break;
                case QuestionType.END_WITH_SUMMARY:
                case QuestionType.END_WITHOUT_SUMMARY:
                case QuestionType.ERROR:
                default:
                    break;
            }
        }
    }
}

public class ValidatedTemplate(ulong categoryID, ValidatedInterviewQuestion interview)
{
    [JsonProperty("category-id", Required = Required.Always)]
    public ulong categoryID = categoryID;

    [JsonProperty("interview", Required = Required.Always)]
    public ValidatedInterviewQuestion interview = interview;
}