using System;
using System.Collections.Generic;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SupportBoi.Interviews;

namespace SupportBoi.Database;

public static class Interviews
{
    public static bool TryGetInterviewTemplateJSON(ulong categoryID, out string templateJSON)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM interview_templates WHERE category_id=@category_id", c);
        selection.Parameters.AddWithValue("@category_id", categoryID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            templateJSON = null;
            return false;
        }

        templateJSON = results.GetString("template");
        results.Close();
        return true;
    }

    public static bool TryGetInterviewFromTemplate(ulong categoryID, ulong channelID, out Interview interview)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM interview_templates WHERE category_id=@category_id", c);
        selection.Parameters.AddWithValue("@category_id", categoryID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if messages exist in the database
        if (!results.Read())
        {
            interview = null;
            return false;
        }

        string templateString = results.GetString("template");
        results.Close();

        try
        {
            Template template = JsonConvert.DeserializeObject<Template>(templateString, new JsonSerializerSettings
            {
                Error = delegate (object _, ErrorEventArgs args)
                {
                    Logger.Error("Error occured when trying to read interview template '" + categoryID + "' from database: " + args.ErrorContext.Error.Message);
                    Logger.Debug("Detailed exception:", args.ErrorContext.Error);
                    args.ErrorContext.Handled = false;
                }
            });
            interview = new Interview(channelID, template.interview, template.definitions);
            return true;
        }
        catch (Exception e)
        {
            Logger.Warn("Unable to create interview object from the current template for category '" + categoryID + "' in the database.", e);
            interview = null;
            return false;
        }
    }

    public static bool SetInterviewTemplate(Template template)
    {
        try
        {
            string templateString = JsonConvert.SerializeObject(template, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Error,
                Formatting = Formatting.Indented,
                ContractResolver = new InterviewStep.StripInternalPropertiesResolver()
            });

            string query;
            if (TryGetInterviewTemplateJSON(template.categoryID, out _))
            {
                query = "UPDATE interview_templates SET template = @template WHERE category_id=@category_id";
            }
            else
            {
                query = "INSERT INTO interview_templates (category_id,template) VALUES (@category_id, @template)";
            }

            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new(query, c);
            cmd.Parameters.AddWithValue("@category_id", template.categoryID);
            cmd.Parameters.AddWithValue("@template", templateString);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Error("Could not set interview template in database.", e);
            return false;
        }
    }

    public static bool TryDeleteInterviewTemplate(ulong categoryID)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand deletion = new("DELETE FROM interview_templates WHERE category_id=@category_id", c);
            deletion.Parameters.AddWithValue("@category_id", categoryID);
            deletion.Prepare();
            return deletion.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool SaveInterview(Interview interview)
    {
        try
        {
            string query;
            if (TryGetInterview(interview.channelID, out _))
            {
                query = "UPDATE interviews SET interview = @interview, definitions = @definitions WHERE channel_id = @channel_id";
            }
            else
            {
                query = "INSERT INTO interviews (channel_id,interview, definitions) VALUES (@channel_id, @interview, @definitions)";
            }

            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new(query, c);
            cmd.Parameters.AddWithValue("@channel_id", interview.channelID);
            cmd.Parameters.AddWithValue("@interview", JsonConvert.SerializeObject(interview.interviewRoot, Formatting.Indented));
            cmd.Parameters.AddWithValue("@definitions", JsonConvert.SerializeObject(interview.definitions, Formatting.Indented));
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Error("Could not save interview to database.", e);
            return false;
        }
    }

    public static bool TryGetInterview(ulong channelID, out Interview interview)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM interviews WHERE channel_id=@channel_id", c);
        selection.Parameters.AddWithValue("@channel_id", channelID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            interview = null;
            return false;
        }
        interview = new Interview(channelID,
                                  JsonConvert.DeserializeObject<InterviewStep>(results.GetString("interview")),
                                  JsonConvert.DeserializeObject<Dictionary<string, InterviewStep>>(results.GetString("definitions")));
        results.Close();
        return true;
    }

    public static bool TryDeleteInterview(ulong channelID)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand deletion = new("DELETE FROM interviews WHERE channel_id=@channel_id", c);
            deletion.Parameters.AddWithValue("@channel_id", channelID);
            deletion.Prepare();
            return deletion.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }
}