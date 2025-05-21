using System.Collections.Generic;
using MySqlConnector;

namespace SupportBoi.Database;

public class Message(MySqlDataReader reader)
{
    public readonly string identifier = reader.GetString("identifier");
    public readonly ulong userID = reader.GetUInt64("user_id");
    public readonly string message = reader.GetString("message");

    public static List<Message> GetAllMessages()
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM messages", c);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return [];
        }

        List<Message> messages = [new(results)];
        while (results.Read())
        {
            messages.Add(new Message(results));
        }
        results.Close();

        return messages;
    }

    public static List<string> GetIDs()
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT `identifier` FROM messages", c);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return [];
        }

        List<string> messages = [results.GetString("identifier")];
        while (results.Read())
        {
            messages.Add(results.GetString("identifier"));
        }
        results.Close();

        return messages;
    }

    public static bool TryGetMessage(string identifier, out Message message)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM messages WHERE identifier=@identifier", c);
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
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new("INSERT INTO messages (identifier,user_id,message) VALUES (@identifier, @user_id, @message);", c);
            cmd.Parameters.AddWithValue("@identifier", identifier);
            cmd.Parameters.AddWithValue("@user_id", userID);
            cmd.Parameters.AddWithValue("@message", message);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Error("Could not add message to database.", e);
            return false;
        }
    }

    public static bool UpdateMessage(string identifier, ulong userID, string message)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new("UPDATE messages SET message = @message, user_id = @user_id WHERE identifier=@identifier", c);
            cmd.Parameters.AddWithValue("@identifier", identifier);
            cmd.Parameters.AddWithValue("@user_id", userID);
            cmd.Parameters.AddWithValue("@message", message);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Error("Could not add message to database.", e);
            return false;
        }
    }

    public static bool RemoveMessage(string identifier)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new("DELETE FROM messages WHERE identifier=@identifier", c);
            cmd.Parameters.AddWithValue("@identifier", identifier);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Error("Could not remove message from database.", e);
            return false;
        }
    }
}