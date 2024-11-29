using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using DSharpPlus;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SupportBoi.Interviews;

namespace SupportBoi;

public static class Database
{
    private static string connectionString = "";

    public static void SetConnectionString(string host, int port, string database, string username, string password)
    {
        connectionString = "server=" + host +
                           ";database=" + database +
                           ";port=" + port +
                           ";userid=" + username +
                           ";password=" + password;
    }

    public static MySqlConnection GetConnection()
    {
        return new MySqlConnection(connectionString);
    }

    public static long GetNumberOfTickets()
    {
        try
        {
            using MySqlConnection c = GetConnection();
            using MySqlCommand countTickets = new MySqlCommand("SELECT COUNT(*) FROM tickets", c);
            c.Open();
            return (long)countTickets.ExecuteScalar();
        }
        catch (Exception e)
        {
            Logger.Error("Error occured when attempting to count number of open tickets: " + e);
        }

        return -1;
    }

    public static long GetNumberOfClosedTickets()
    {
        try
        {
            using MySqlConnection c = GetConnection();
            using MySqlCommand countTickets = new MySqlCommand("SELECT COUNT(*) FROM ticket_history", c);
            c.Open();
            return (long)countTickets.ExecuteScalar();
        }
        catch (Exception e)
        {
            Logger.Error("Error occured when attempting to count number of open tickets: " + e);
        }

        return -1;
    }

    public static void SetupTables()
    {
        using MySqlConnection c = GetConnection();
        using MySqlCommand createTickets = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS tickets(" +
            "id INT UNSIGNED NOT NULL PRIMARY KEY AUTO_INCREMENT," +
            "created_time DATETIME NOT NULL," +
            "creator_id BIGINT UNSIGNED NOT NULL," +
            "assigned_staff_id BIGINT UNSIGNED NOT NULL DEFAULT 0," +
            "summary VARCHAR(5000) NOT NULL," +
            "channel_id BIGINT UNSIGNED NOT NULL UNIQUE," +
            "INDEX(created_time, assigned_staff_id, channel_id))",
            c);
        using MySqlCommand createTicketHistory = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS ticket_history(" +
            "id INT UNSIGNED NOT NULL PRIMARY KEY," +
            "created_time DATETIME NOT NULL," +
            "closed_time DATETIME NOT NULL," +
            "creator_id BIGINT UNSIGNED NOT NULL," +
            "assigned_staff_id BIGINT UNSIGNED NOT NULL DEFAULT 0," +
            "summary VARCHAR(5000) NOT NULL," +
            "channel_id BIGINT UNSIGNED NOT NULL UNIQUE," +
            "INDEX(created_time, closed_time, channel_id))",
            c);
        using MySqlCommand createBlacklisted = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS blacklisted_users(" +
            "user_id BIGINT UNSIGNED NOT NULL PRIMARY KEY," +
            "time DATETIME NOT NULL," +
            "moderator_id BIGINT UNSIGNED NOT NULL," +
            "INDEX(user_id, time))",
            c);
        using MySqlCommand createStaffList = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS staff(" +
            "user_id BIGINT UNSIGNED NOT NULL PRIMARY KEY," +
            "name VARCHAR(256) NOT NULL," +
            "active BOOLEAN NOT NULL DEFAULT true)",
            c);
        using MySqlCommand createMessages = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS messages(" +
            "identifier VARCHAR(256) NOT NULL PRIMARY KEY," +
            "user_id BIGINT UNSIGNED NOT NULL," +
            "message VARCHAR(5000) NOT NULL)",
            c);
        using MySqlCommand createCategories = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS categories(" +
            "name VARCHAR(256) NOT NULL UNIQUE," +
            "category_id BIGINT UNSIGNED NOT NULL PRIMARY KEY)",
            c);
        using MySqlCommand createInterviews = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS interviews(" +
            "channel_id BIGINT UNSIGNED NOT NULL PRIMARY KEY," +
            "interview JSON NOT NULL)",
            c);
        using MySqlCommand createInterviewTemplates = new MySqlCommand(
            "CREATE TABLE IF NOT EXISTS interview_templates(" +
            "category_id BIGINT UNSIGNED NOT NULL PRIMARY KEY," +
            "template JSON NOT NULL)",
            c);
        c.Open();
        createTickets.ExecuteNonQuery();
        createBlacklisted.ExecuteNonQuery();
        createTicketHistory.ExecuteNonQuery();
        createStaffList.ExecuteNonQuery();
        createMessages.ExecuteNonQuery();
        createCategories.ExecuteNonQuery();
        createInterviews.ExecuteNonQuery();
        createInterviewTemplates.ExecuteNonQuery();
    }

    public static bool IsOpenTicket(ulong channelID)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE channel_id=@channel_id", c);
        selection.Parameters.AddWithValue("@channel_id", channelID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            return false;
        }
        results.Close();
        return true;
    }

    public static bool TryGetOpenTicket(ulong channelID, out Ticket ticket)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE channel_id=@channel_id", c);
        selection.Parameters.AddWithValue("@channel_id", channelID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            ticket = null;
            return false;
        }

        ticket = new Ticket(results);
        results.Close();
        return true;
    }

    public static bool TryGetOpenTicketByID(uint id, out Ticket ticket)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE id=@id", c);
        selection.Parameters.AddWithValue("@id", id);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if open ticket exists in the database
        if (results.Read())
        {
            ticket = new Ticket(results);
            results.Close();
            return true;
        }

        results.Close();
        ticket = null;
        return false;
    }

    public static bool TryGetClosedTicket(uint id, out Ticket ticket)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM ticket_history WHERE id=@id", c);
        selection.Parameters.AddWithValue("@id", id);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if closed ticket exists in the database
        if (results.Read())
        {
            ticket = new Ticket(results);
            results.Close();
            return true;
        }

        ticket = null;
        results.Close();
        return false;
    }

    public static bool TryGetOpenTickets(ulong userID, out List<Ticket> tickets)
    {
        tickets = null;
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE creator_id=@creator_id", c);
        selection.Parameters.AddWithValue("@creator_id", userID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }

        tickets = new List<Ticket> { new Ticket(results) };
        while (results.Read())
        {
            tickets.Add(new Ticket(results));
        }
        results.Close();
        return true;
    }

    public static bool TryGetOpenTickets(out List<Ticket> tickets)
    {
        tickets = null;
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets ORDER BY channel_id ASC", c);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }

        tickets = new List<Ticket> { new Ticket(results) };
        while (results.Read())
        {
            tickets.Add(new Ticket(results));
        }
        results.Close();
        return true;
    }

    public static bool TryGetClosedTickets(ulong userID, out List<Ticket> tickets)
    {
        tickets = null;
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM ticket_history WHERE creator_id=@creator_id", c);
        selection.Parameters.AddWithValue("@creator_id", userID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }

        tickets = new List<Ticket> { new Ticket(results) };
        while (results.Read())
        {
            tickets.Add(new Ticket(results));
        }
        results.Close();
        return true;
    }

    public static bool TryGetAssignedTickets(ulong staffID, out List<Ticket> tickets)
    {
        tickets = null;
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM tickets WHERE assigned_staff_id=@assigned_staff_id", c);
        selection.Parameters.AddWithValue("@assigned_staff_id", staffID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }

        tickets = new List<Ticket> { new Ticket(results) };
        while (results.Read())
        {
            tickets.Add(new Ticket(results));
        }
        results.Close();
        return true;
    }

    public static long NewTicket(ulong memberID, ulong staffID, ulong ticketID)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand cmd = new MySqlCommand(@"INSERT INTO tickets (created_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (UTC_TIMESTAMP(), @creator_id, @assigned_staff_id, @summary, @channel_id);", c);
        cmd.Parameters.AddWithValue("@creator_id", memberID);
        cmd.Parameters.AddWithValue("@assigned_staff_id", staffID);
        cmd.Parameters.AddWithValue("@summary", "");
        cmd.Parameters.AddWithValue("@channel_id", ticketID);
        cmd.ExecuteNonQuery();
        return cmd.LastInsertedId;
    }

    public static void ArchiveTicket(Ticket ticket)
    {
        // Check if ticket already exists in the archive
        if (TryGetClosedTicket(ticket.id, out Ticket _))
        {
            using MySqlConnection c = GetConnection();
            using MySqlCommand deleteTicket = new MySqlCommand(@"DELETE FROM ticket_history WHERE id=@id OR channel_id=@channel_id", c);
            deleteTicket.Parameters.AddWithValue("@id", ticket.id);
            deleteTicket.Parameters.AddWithValue("@channel_id", ticket.channelID);

            c.Open();
            deleteTicket.Prepare();
            deleteTicket.ExecuteNonQuery();
        }

        // Create an entry in the ticket history database
        using MySqlConnection conn = GetConnection();
        using MySqlCommand archiveTicket = new MySqlCommand(@"INSERT INTO ticket_history (id, created_time, closed_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (@id, @created_time, UTC_TIMESTAMP(), @creator_id, @assigned_staff_id, @summary, @channel_id);", conn);
        archiveTicket.Parameters.AddWithValue("@id", ticket.id);
        archiveTicket.Parameters.AddWithValue("@created_time", ticket.channelID.GetSnowflakeTime());
        archiveTicket.Parameters.AddWithValue("@creator_id", ticket.creatorID);
        archiveTicket.Parameters.AddWithValue("@assigned_staff_id", ticket.assignedStaffID);
        archiveTicket.Parameters.AddWithValue("@summary", ticket.summary);
        archiveTicket.Parameters.AddWithValue("@channel_id", ticket.channelID);

        conn.Open();
        archiveTicket.Prepare();
        archiveTicket.ExecuteNonQuery();
    }

    public static bool DeleteOpenTicket(uint ticketID)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            using MySqlCommand deletion = new MySqlCommand(@"DELETE FROM tickets WHERE id=@id", c);
            deletion.Parameters.AddWithValue("@id", ticketID);

            c.Open();
            deletion.Prepare();
            return deletion.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool IsBlacklisted(ulong userID)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM blacklisted_users WHERE user_id=@user_id", c);
        selection.Parameters.AddWithValue("@user_id", userID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if user is blacklisted
        if (results.Read())
        {
            results.Close();
            return true;
        }
        results.Close();

        return false;
    }

    public static bool Blacklist(ulong blacklistedID, ulong staffID)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"INSERT INTO blacklisted_users (user_id,time,moderator_id) VALUES (@user_id, UTC_TIMESTAMP(), @moderator_id);", c);
            cmd.Parameters.AddWithValue("@user_id", blacklistedID);
            cmd.Parameters.AddWithValue("@moderator_id", staffID);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool Unblacklist(ulong blacklistedID)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"DELETE FROM blacklisted_users WHERE user_id=@user_id", c);
            cmd.Parameters.AddWithValue("@user_id", blacklistedID);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool AssignStaff(Ticket ticket, ulong staffID)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand update = new MySqlCommand(@"UPDATE tickets SET assigned_staff_id = @assigned_staff_id WHERE id = @id", c);
            update.Parameters.AddWithValue("@assigned_staff_id", staffID);
            update.Parameters.AddWithValue("@id", ticket.id);
            update.Prepare();
            return update.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool UnassignStaff(Ticket ticket)
    {
        return AssignStaff(ticket, 0);
    }

    public static bool SetStaffActive(ulong staffID, bool active)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            MySqlCommand update = new MySqlCommand(@"UPDATE staff SET active = @active WHERE user_id = @user_id", c);
            update.Parameters.AddWithValue("@user_id", staffID);
            update.Parameters.AddWithValue("@active", active);
            update.Prepare();
            return update.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static StaffMember GetRandomActiveStaff(params ulong[] ignoredUserIDs)
    {
        List<StaffMember> staffMembers = GetActiveStaff(ignoredUserIDs);
        return staffMembers.Any() ? staffMembers[RandomNumberGenerator.GetInt32(staffMembers.Count)] : null;
    }

    public static List<StaffMember> GetActiveStaff(params ulong[] ignoredUserIDs)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE active = true AND user_id NOT IN (@user_ids)", c);
        selection.Parameters.AddWithValue("@user_ids", string.Join(",", ignoredUserIDs));
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if staff exists in the database
        if (!results.Read())
        {
            return new List<StaffMember>();
        }

        List<StaffMember> staffMembers = new List<StaffMember> { new StaffMember(results) };
        while (results.Read())
        {
            staffMembers.Add(new StaffMember(results));
        }
        results.Close();

        return staffMembers;
    }

    public static List<StaffMember> GetAllStaff(params ulong[] ignoredUserIDs)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE user_id NOT IN (@user_ids)", c);
        selection.Parameters.AddWithValue("@user_ids", string.Join(",", ignoredUserIDs));
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if staff exist in the database
        if (!results.Read())
        {
            return new List<StaffMember>();
        }

        List<StaffMember> staffMembers = new List<StaffMember> { new StaffMember(results) };
        while (results.Read())
        {
            staffMembers.Add(new StaffMember(results));
        }
        results.Close();

        return staffMembers;
    }

    public static bool IsStaff(ulong staffID)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE user_id=@user_id", c);
        selection.Parameters.AddWithValue("@user_id", staffID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            return false;
        }
        results.Close();
        return true;
    }

    public static bool TryGetStaff(ulong staffID, out StaffMember staffMember)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM staff WHERE user_id=@user_id", c);
        selection.Parameters.AddWithValue("@user_id", staffID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            staffMember = null;
            return false;
        }
        staffMember = new StaffMember(results);
        results.Close();
        return true;
    }

    public static List<Message> GetAllMessages()
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM messages", c);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if messages exist in the database
        if (!results.Read())
        {
            return new List<Message>();
        }

        List<Message> messages = new List<Message> { new Message(results) };
        while (results.Read())
        {
            messages.Add(new Message(results));
        }
        results.Close();

        return messages;
    }

    public static bool TryGetMessage(string identifier, out Message message)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM messages WHERE identifier=@identifier", c);
        selection.Parameters.AddWithValue("@identifier", identifier);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            message = null;
            return false;
        }
        message = new Message(results);
        results.Close();
        return true;
    }

    public static bool AddMessage(string identifier, ulong userID, string message)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"INSERT INTO messages (identifier,user_id,message) VALUES (@identifier, @user_id, @message);", c);
            cmd.Parameters.AddWithValue("@identifier", identifier);
            cmd.Parameters.AddWithValue("@user_id", userID);
            cmd.Parameters.AddWithValue("@message", message);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool RemoveMessage(string identifier)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"DELETE FROM messages WHERE identifier=@identifier", c);
            cmd.Parameters.AddWithValue("@identifier", identifier);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static List<Category> GetAllCategories()
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM categories", c);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if messages exist in the database
        if (!results.Read())
        {
            return new List<Category>();
        }

        List<Category> categories = new List<Category> { new Category(results) };
        while (results.Read())
        {
            categories.Add(new Category(results));
        }
        results.Close();

        return categories;
    }

    public static bool TryGetCategory(ulong categoryID, out Category message)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM categories WHERE category_id=@category_id", c);
        selection.Parameters.AddWithValue("@category_id", categoryID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            message = null;
            return false;
        }
        message = new Category(results);
        results.Close();
        return true;
    }

    public static bool TryGetCategory(string name, out Category message)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM categories WHERE name=@name", c);
        selection.Parameters.AddWithValue("@name", name);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            message = null;
            return false;
        }
        message = new Category(results);
        results.Close();
        return true;
    }

    public static bool AddCategory(string name, ulong categoryID)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"INSERT INTO categories (name,category_id) VALUES (@name, @category_id);", c);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@category_id", categoryID);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool RemoveCategory(ulong categoryID)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"DELETE FROM categories WHERE category_id=@category_id", c);
            cmd.Parameters.AddWithValue("@category_id", categoryID);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static string GetInterviewTemplateJSON(ulong categoryID)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand("SELECT * FROM interview_templates WHERE category_id=@category_id", c);
        selection.Parameters.AddWithValue("@category_id", categoryID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Return a default template if it doesn't exist.
        if (!results.Read())
        {
            return null;
        }

        string templates = results.GetString("template");
        results.Close();
        return templates;
    }

    public static bool TryGetInterviewTemplate(ulong categoryID, out Interviews.InterviewStep template)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand("SELECT * FROM interview_templates WHERE category_id=@category_id", c);
        selection.Parameters.AddWithValue("@category_id", categoryID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if messages exist in the database
        if (!results.Read())
        {
            template = null;
            return false;
        }

        string templateString = results.GetString("template");
        results.Close();

        try
        {
            template = JsonConvert.DeserializeObject<Interviews.Template>(templateString, new JsonSerializerSettings
            {
                Error = delegate (object sender, ErrorEventArgs args)
                {
                    Logger.Error("Error occured when trying to read interview template '" + categoryID + "' from database: " + args.ErrorContext.Error.Message);
                    Logger.Debug("Detailed exception:", args.ErrorContext.Error);
                    args.ErrorContext.Handled = false;
                }
            }).interview;
            return true;
        }
        catch (Exception)
        {
            template = null;
            return false;
        }
    }

    public static bool SetInterviewTemplate(Interviews.Template template)
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
            if (TryGetInterviewTemplate(template.categoryID, out _))
            {
                query = "UPDATE interview_templates SET template = @template WHERE category_id=@category_id";
            }
            else
            {
                query = "INSERT INTO interview_templates (category_id,template) VALUES (@category_id, @template)";
            }

            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(query, c);
            cmd.Parameters.AddWithValue("@category_id", template.categoryID);
            cmd.Parameters.AddWithValue("@template", templateString);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool TryDeleteInterviewTemplate(ulong categoryID)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand deletion = new MySqlCommand(@"DELETE FROM interview_templates WHERE category_id=@category_id", c);
            deletion.Parameters.AddWithValue("@category_id", categoryID);
            deletion.Prepare();
            return deletion.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool SaveInterview(ulong channelID, Interviews.InterviewStep interview)
    {
        try
        {
            string query;
            if (TryGetInterview(channelID, out _))
            {
                query = "UPDATE interviews SET interview = @interview WHERE channel_id = @channel_id";
            }
            else
            {
                query = "INSERT INTO interviews (channel_id,interview) VALUES (@channel_id, @interview)";
            }

            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(query, c);
            cmd.Parameters.AddWithValue("@channel_id", channelID);
            cmd.Parameters.AddWithValue("@interview", JsonConvert.SerializeObject(interview, Formatting.Indented));
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public static bool TryGetInterview(ulong channelID, out Interviews.InterviewStep interview)
    {
        using MySqlConnection c = GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM interviews WHERE channel_id=@channel_id", c);
        selection.Parameters.AddWithValue("@channel_id", channelID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        // Check if ticket exists in the database
        if (!results.Read())
        {
            interview = null;
            return false;
        }
        interview = JsonConvert.DeserializeObject<Interviews.InterviewStep>(results.GetString("interview"));
        results.Close();
        return true;
    }

    public static bool TryDeleteInterview(ulong channelID)
    {
        try
        {
            using MySqlConnection c = GetConnection();
            c.Open();
            using MySqlCommand deletion = new MySqlCommand(@"DELETE FROM interviews WHERE channel_id=@channel_id", c);
            deletion.Parameters.AddWithValue("@channel_id", channelID);
            deletion.Prepare();
            return deletion.ExecuteNonQuery() > 0;
        }
        catch (MySqlException)
        {
            return false;
        }
    }

    public class Ticket
    {
        public uint id;
        public ulong creatorID;
        public ulong assignedStaffID;
        public string summary;
        public ulong channelID;

        public Ticket(MySqlDataReader reader)
        {
            id = reader.GetUInt32("id");
            creatorID = reader.GetUInt64("creator_id");
            assignedStaffID = reader.GetUInt64("assigned_staff_id");
            summary = reader.GetString("summary");
            channelID = reader.GetUInt64("channel_id");
        }

        public string DiscordRelativeTime()
        {
            return Formatter.Timestamp(channelID.GetSnowflakeTime(), Config.timestampFormat);
        }
    }
    public class StaffMember
    {
        public ulong userID;
        public string name;
        public bool active;

        public StaffMember(MySqlDataReader reader)
        {
            userID = reader.GetUInt64("user_id");
            name = reader.GetString("name");
            active = reader.GetBoolean("active");
        }
    }

    public class Message
    {
        public string identifier;
        public ulong userID;
        public string message;

        public Message(MySqlDataReader reader)
        {
            identifier = reader.GetString("identifier");
            userID = reader.GetUInt64("user_id");
            message = reader.GetString("message");
        }
    }

    public class Category
    {
        public string name;
        public ulong id;

        public Category(MySqlDataReader reader)
        {
            name = reader.GetString("name");
            id = reader.GetUInt64("category_id");
        }
    }
}