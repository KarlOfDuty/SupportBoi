using System.Collections.Generic;
using DSharpPlus;
using MySqlConnector;

namespace SupportBoi.Database;

public class Ticket(MySqlDataReader reader)
{
    public readonly uint id = reader.GetUInt32("id");
    public readonly ulong creatorID = reader.GetUInt64("creator_id");
    public readonly ulong assignedStaffID = reader.GetUInt64("assigned_staff_id");
    public readonly string summary = reader.GetString("summary");
    public readonly ulong channelID = reader.GetUInt64("channel_id");

    public string DiscordRelativeTime()
    {
        return Formatter.Timestamp(channelID.GetSnowflakeTime(), Config.timestampFormat);
    }

    public static long GetNumberOfTickets()
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            using MySqlCommand countTickets = new MySqlCommand("SELECT COUNT(*) FROM tickets", c);
            c.Open();
            return (long)(countTickets?.ExecuteScalar() ?? 0);
        }
        catch (MySqlException e)
        {
            Logger.Error("Error occured when attempting to count number of open tickets.", e);
        }

        return -1;
    }

    public static long GetNumberOfClosedTickets()
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            using MySqlCommand countTickets = new MySqlCommand("SELECT COUNT(*) FROM ticket_history", c);
            c.Open();
            return (long)(countTickets?.ExecuteScalar() ?? 0);
        }
        catch (MySqlException e)
        {
            Logger.Error("Error occured when attempting to count number of open tickets.", e);
        }

        return -1;
    }

    public static bool IsOpenTicket(ulong channelID)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM tickets WHERE channel_id=@channel_id", c);
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
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM tickets WHERE channel_id=@channel_id", c);
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
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM tickets WHERE id=@id", c);
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
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM ticket_history WHERE id=@id", c);
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
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM tickets WHERE creator_id=@creator_id", c);
        selection.Parameters.AddWithValue("@creator_id", userID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }

        tickets = [new Ticket(results)];
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
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM tickets ORDER BY channel_id", c);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }

        tickets = [new Ticket(results)];
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
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM ticket_history WHERE creator_id=@creator_id", c);
        selection.Parameters.AddWithValue("@creator_id", userID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }

        tickets = [new Ticket(results)];
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
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM tickets WHERE assigned_staff_id=@assigned_staff_id", c);
        selection.Parameters.AddWithValue("@assigned_staff_id", staffID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }

        tickets = [new Ticket(results)];
        while (results.Read())
        {
            tickets.Add(new Ticket(results));
        }
        results.Close();
        return true;
    }

    public static long NewTicket(ulong memberID, ulong staffID, ulong ticketID)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand cmd = new("INSERT INTO tickets (created_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (UTC_TIMESTAMP(), @creator_id, @assigned_staff_id, @summary, @channel_id);", c);
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
            using MySqlConnection c = Connection.GetConnection();
            using MySqlCommand deleteTicket = new("DELETE FROM ticket_history WHERE id=@id OR channel_id=@channel_id", c);
            deleteTicket.Parameters.AddWithValue("@id", ticket.id);
            deleteTicket.Parameters.AddWithValue("@channel_id", ticket.channelID);

            c.Open();
            deleteTicket.Prepare();
            deleteTicket.ExecuteNonQuery();
        }

        // Create an entry in the ticket history database
        using MySqlConnection conn = Connection.GetConnection();
        using MySqlCommand archiveTicket = new("INSERT INTO ticket_history (id, created_time, closed_time, creator_id, assigned_staff_id, summary, channel_id) VALUES (@id, @created_time, UTC_TIMESTAMP(), @creator_id, @assigned_staff_id, @summary, @channel_id);", conn);
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
            using MySqlConnection c = Connection.GetConnection();
            using MySqlCommand deletion = new("DELETE FROM tickets WHERE id=@id", c);
            deletion.Parameters.AddWithValue("@id", ticketID);

            c.Open();
            deletion.Prepare();
            return deletion.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Warn("Could not delete open ticket in database.", e);
            return false;
        }
    }

    public static bool SetSummary(ulong channelID, string summary)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand update = new("UPDATE tickets SET summary = @summary WHERE channel_id = @channel_id", c);
            update.Parameters.AddWithValue("@summary", summary);
            update.Parameters.AddWithValue("@channel_id", channelID);
            update.Prepare();
            return update.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Warn("Could not set summary in database.", e);
            return false;
        }
    }
}