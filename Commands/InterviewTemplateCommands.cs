using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
[Description("Administrative commands.")]
public class InterviewTemplateCommands
{
    private static string jsonSchema = Utilities.ReadManifestData("Interviews.interview_template.schema.json");

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

        if (!Database.TryGetCategory(category.Id, out Database.Category categoryData))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That category is not registered with the bot."
            }, true);
            return;
        }

        string interviewTemplateJSON = Database.GetInterviewTemplateJSON(category.Id);
        if (interviewTemplateJSON == null)
        {
            string defaultTemplate =
            "{\n" +
            "  \"category-id\": \"" + category.Id + "\",\n" +
            "  \"interview\":\n" +
            "  {\n" +
            "    \"message\": \"\",\n" +
            "    \"type\": \"\",\n" +
            "    \"color\": \"\",\n" +
            "    \"paths\":\n" +
            "    {\n" +
            "      \n" +
            "    }\n" +
            "  }\n" +
            "}";
            MemoryStream stream = new(Encoding.UTF8.GetBytes(defaultTemplate));

            DiscordInteractionResponseBuilder response = new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = "No interview template found for this category. A default template has been generated."
            }).AddFile("interview-template-" + category.Id + ".json", stream).AsEphemeral();
            await command.RespondAsync(response);
        }
        else
        {
            MemoryStream stream = new(Encoding.UTF8.GetBytes(interviewTemplateJSON));
            await command.RespondAsync(new DiscordInteractionResponseBuilder().AddFile("interview-template-" + category.Id + ".json", stream).AsEphemeral());
        }
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

        try
        {
            List<string> errors = [];

            Stream stream = await new HttpClient().GetStreamAsync(file.Url);
            JSchemaValidatingReader validatingReader = new(new JsonTextReader(new StreamReader(stream)));
            validatingReader.Schema = JSchema.Parse(jsonSchema);

            // The schema seems to throw an additional error with incorrect information if an invalid parameter is included
            // in the template. Throw here in order to only show the first correct error to the user, also skips unnecessary validation further down.
            validatingReader.ValidationEventHandler += (o, a) => throw new JsonException(a.Message);

            JsonSerializer serializer = new();
            Template template = serializer.Deserialize<Template>(validatingReader);

            DiscordChannel category = await SupportBoi.client.GetChannelAsync(template.categoryID);
            if (!category.IsCategory)
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The category ID in the uploaded JSON structure is not a valid category."
                }, true);
                return;
            }

            if (!Database.TryGetCategory(category.Id, out Database.Category _))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The category ID in the uploaded JSON structure is not a category registered with the bot, use /addcategory first."
                }, true);
                return;
            }

            template.interview.Validate(ref errors, out int summaryCount, out int summaryMaxLength);

            if (summaryCount > 25)
            {
                errors.Add("A summary cannot contain more than 25 fields, but you have " + summaryCount + " fields in at least one of your interview branches.");
            }

            if (summaryMaxLength >= 6000)
            {
                errors.Add("A summary cannot contain more than 6000 characters, but at least one of your branches has the possibility of its summary reaching " + summaryMaxLength + " characters.");
            }

            if (errors.Count != 0)
            {
                string errorString = string.Join("\n\n", errors);
                if (errorString.Length > 1500)
                {
                    errorString = errorString.Substring(0, 1500);
                }

                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "The uploaded JSON structure could not be parsed as an interview template.\n\nErrors:\n```\n" + errorString + "\n```"
                }, true);
                return;
            }

            if (!Database.SetInterviewTemplate(template))
            {
                await command.RespondAsync(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Description = "An error occured trying to write the new template to database."
                }, true);
                return;
            }

            try
            {
                MemoryStream memStream = new(Encoding.UTF8.GetBytes(Database.GetInterviewTemplateJSON(template.categoryID)));

                // Log it if the log channel exists
                DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
                await logChannel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Green,
                    Description = command.User.Mention + " uploaded a new interview template for the `" + category.Name + "` category."
                }).AddFile("interview-template-" + template.categoryID + ".json", memStream));
            }
            catch (NotFoundException)
            {
                Logger.Error("Could not find the log channel.");
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

        await command.RespondAsync(new DiscordEmbedBuilder
        {
            Color = DiscordColor.Green,
            Description = "Uploaded interview template."
        }, true);
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

        if (!Database.TryGetCategory(category.Id, out Database.Category _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That category is not registered with the bot."
            }, true);
            return;
        }

        if (!Database.TryGetInterviewTemplate(category.Id, out InterviewQuestion _))
        {
            await command.RespondAsync(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Red,
                Description = "That category does not have an interview template."
            }, true);
            return;
        }

        MemoryStream memStream = new(Encoding.UTF8.GetBytes(Database.GetInterviewTemplateJSON(category.Id)));
        if (!Database.TryDeleteInterviewTemplate(category.Id))
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

        try
        {
            // Log it if the log channel exists
            DiscordChannel logChannel = await SupportBoi.client.GetChannelAsync(Config.logChannel);
            await logChannel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Color = DiscordColor.Green,
                Description = command.User.Mention + " deleted the interview template for the `" + category.Name +"` category."
            }).AddFile("interview-template-" + category.Id + ".json", memStream));
        }
        catch (NotFoundException)
        {
            Logger.Error("Could not find the log channel.");
        }
    }
}