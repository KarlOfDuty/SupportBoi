using MySqlConnector;

namespace SupportBoi.Database;

public class Blacklist
{
    public static bool IsBanned(ulong userID)
    {
        using MySqlConnection c = Connection.GetConnection();
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

    public static bool Ban(ulong blacklistedID, ulong staffID)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"INSERT INTO blacklisted_users (user_id,time,moderator_id) VALUES (@user_id, UTC_TIMESTAMP(), @moderator_id);", c);
            cmd.Parameters.AddWithValue("@user_id", blacklistedID);
            cmd.Parameters.AddWithValue("@moderator_id", staffID);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Warn("Could not blacklist user in database.", e);
            return false;
        }
    }

    public static bool Unban(ulong blacklistedID)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"DELETE FROM blacklisted_users WHERE user_id=@user_id", c);
            cmd.Parameters.AddWithValue("@user_id", blacklistedID);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Warn("Could not unblacklist user in database.", e);
            return false;
        }
    }
}