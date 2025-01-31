using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using SupportBoi.Interviews;

namespace SupportBoi.Commands;

[Command("interviewtemplate")]
[Description("Interview template management.")]
public class InterviewTemplateCommands
{
    private static readonly string jsonSchema = Utilities.ReadManifestData("Interviews.interview_template.schema.json");

    [RequireGuild]
    [Command("get")]
    [Description("Provides a copy of the interview template for a category which you can edit and then reupload.")]
    public async Task Get(SlashCommandContext command,
        [Parameter("category")] [Description("The category to get the template for.")] DiscordChannel category)
    {
        if (!category?.IsCategory ?? true)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That channel is not a category."
            }, true);
            return;
        }

        if (Database.Interviews.TryGetInterviewTemplateJSON(category.Id, out string templateJSON))
        {
            MemoryStream stream = new(Encoding.UTF8.GetBytes(templateJSON));
            await command.RespondAsync(new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "Upload the json file using the `/interviewtemplate set` command when you are done editing it.\n\n" +
                                  "Click [here](https://github.com/KarlOfDuty/SupportBoi/blob/main/docs/InterviewTemplates.md) to learn how to edit interview templates."
                })
                .AddFile("interview-template-" + category.Id + ".json", stream)
                .AsEphemeral());
            return;
        }

        string defaultTemplate =
            "{\n" +
            "  \"category-id\": " + category.Id + ",\n" +
            "  \"interview\":\n" +
            "  {\n" +
            "    \"message\": \"\",\n" +
            "    \"step-type\": \"\",\n" +
            "    \"color\": \"\",\n" +
            "    \"steps\":\n" +
            "    {\n" +
            "      \n" +
            "    }\n" +
            "  },\n" +
            "  \"definitions\":\n" +
            "  {\n" +
            "    \n" +
            "  }\n" +
            "}";
        MemoryStream defStream = new(Encoding.UTF8.GetBytes(defaultTemplate));

        DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "No interview template found for this category. A default template has been generated.\n\n" +
                          "Upload the json file using the `/interviewtemplate set` command when you are done editing it.\n\n" +
                          "Click [here](https://github.com/KarlOfDuty/SupportBoi/blob/main/docs/InterviewTemplates.md) to learn how to edit interview templates."
        }).AddFile("interview-template-" + category.Id + ".json", defStream).AsEphemeral();
        await command.RespondAsync(response);
    }

    [RequireGuild]
    [Command("set")]
    [Description("Uploads an interview template file.")]
    public async Task Set(SlashCommandContext command,
        [Parameter("file")] [Description("The file containing the template.")] DiscordAttachment file)
    {
        await command.DeferResponseAsync(true);

        if (!file.MediaType?.Contains("application/json") ?? false)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "The uploaded file is not a JSON file according to Discord."
            }, true);
            return;
        }

        Stream stream = await new HttpClient().GetStreamAsync(file.Url);
        try
        {
            JSchemaValidatingReader validatingReader = new(new JsonTextReader(new StreamReader(stream)));
            validatingReader.Schema = JSchema.Parse(jsonSchema);

            // The schema seems to throw an additional error with incorrect information if an invalid parameter is included
            // in the template. Throw here in order to only show the first correct error to the user, also skips unnecessary validation further down.
            validatingReader.ValidationEventHandler += (_, a) => throw new JsonException(a.Message);

            JsonSerializer serializer = new();
            Template template = serializer.Deserialize<Template>(validatingReader);

            DiscordChannel category;
            try
            {
                category = await SupportBoi.client.GetChannelAsync(template.categoryID);
            }
            catch (Exception e)
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "Could not get the category from the ID in the uploaded JSON structure."
                }, true);
                Logger.Warn("Failed to get template category from ID: " + template.categoryID, e);
                return;
            }

            if (!category.IsCategory)
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The channel ID in the uploaded JSON structure is not a category."
                }, true);
                return;
            }

            List<string> errors = [];
            List<string> warnings = [];
            template.interview.Validate(ref errors, ref warnings, "interview", template.definitions);
            foreach (KeyValuePair<string,InterviewStep> definition in template.definitions ?? [])
            {
                definition.Value.Validate(ref errors, ref warnings, "definitions." + definition.Key, template.definitions);
            }

            List<InterviewStep> allSteps = new();
            template.interview.GetAllSteps(ref allSteps);
            if (allSteps.Any(s => s.stepType is StepType.REFERENCE_END))
            {
                errors.Add("The normal interview tree cannot contain any steps of the 'REFERENCE_END' type, these are only allowed in the 'definitions'.");
            }

            if (errors.Count != 0)
            {
                string errorString = string.Join("```\n```", errors);
                if (errorString.Length > 4000)
                {
                    errorString = errorString.Substring(0, 4000);
                }

                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The uploaded JSON structure could not be parsed as an interview template.\n\nErrors:\n```\n" + errorString + "\n```"
                }, true);
                return;
            }

            if (!Database.Interviews.SetInterviewTemplate(template))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "An error occured trying to write the new template to database."
                }, true);
                return;
            }

            if (warnings.Count == 0)
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = "Uploaded interview template for `" + category.Name + "`."
                }, true);
            }
            else
            {
                string warningString = string.Join("```\n```", warnings);
                if (warningString.Length > 4000)
                {
                    warningString = warningString.Substring(0, 4000);
                }

                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Orange,
                    Description = "Uploaded interview template.\n\n**Warnings:**\n```\n" + warningString + "\n```"
                }, true);
            }

            try
            {
                if (Database.Interviews.TryGetInterviewTemplateJSON(template.categoryID, out string templateJSON))
                {
                    MemoryStream memStream = new(Encoding.UTF8.GetBytes(templateJSON));
                    await LogChannel.Success(command.User.Mention + " uploaded a new interview template for the `" + category.Name + "` category.", 0,
                        new Utilities.File("interview-template-" + template.categoryID + ".json", memStream));
                }
                else
                {
                    Logger.Error("Unable to get interview template from database after upload.");
                }

            }
            catch (Exception e)
            {
                Logger.Error("Unable to log interview template upload.", e);
            }
        }
        catch (Exception e)
        {
            Logger.Debug("Exception occured when trying to upload interview template:\n", e);
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "The uploaded JSON structure could not be parsed as an interview template.\n\nError message:\n```\n" + e.Message + "\n```",
                Footer = new DiscordEmbedBuilder.EmbedFooter
                {
                    Text = "More detailed information may be available as debug messages in the bot logs."
                }
            }, true);
            return;
        }
    }

    [RequireGuild]
    [Command("delete")]
    [Description("Deletes the interview template for a category.")]
    public async Task Delete(SlashCommandContext command,
        [Parameter("category")] [Description("The category to delete the template for.")] DiscordChannel category)
    {
        if (!category?.IsCategory ?? true)
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That channel is not a category."
            }, true);
            return;
        }

        if (!Database.Interviews.TryGetInterviewFromTemplate(category.Id, 0, out Interview _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That category does not have an interview template."
            }, true);
            return;
        }

        if (!Database.Interviews.TryGetInterviewTemplateJSON(category.Id, out string templateJSON))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "Something went wrong reading the interview template from the database."
            }, true);
            return;
        }

        MemoryStream memStream = new(Encoding.UTF8.GetBytes(templateJSON));
        if (!Database.Interviews.TryDeleteInterviewTemplate(category.Id))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "A database error occured trying to delete the interview template."
            }, true);
            return;
        }

        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Deleted interview template."
        }, true);

        await LogChannel.Success(command.User.Mention + " deleted the interview template for the `" + category.Name +"` category.", 0,
            new Utilities.File("interview-template-" + category.Id + ".json", memStream));
    }
}