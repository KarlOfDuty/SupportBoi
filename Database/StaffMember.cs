using System.Collections.Generic;
using MySqlConnector;

namespace SupportBoi.Database;

public class StaffMember(MySqlDataReader reader)
{
    public readonly ulong userID = reader.GetUInt64("user_id");
    public string name = reader.GetString("name");
    public readonly bool active = reader.GetBoolean("active");

    public static bool AddStaff(string name, ulong userID)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand update = IsStaff(userID) ? new MySqlCommand("UPDATE staff SET name = @name WHERE user_id = @user_id", c)
                                                        : new MySqlCommand("INSERT INTO staff (user_id, name) VALUES (@user_id, @name);", c);
            update.Parameters.AddWithValue("@name", name);
            update.Parameters.AddWithValue("@user_id", userID);
            update.Prepare();
            return update.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Warn("Could not add staff to database.", e);
            return false;
        }
    }

    public static bool RemoveStaff(ulong userID)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand update = new("DELETE FROM staff WHERE user_id = @user_id", c);
            update.Parameters.AddWithValue("@user_id", userID);
            update.Prepare();
            return update.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Warn("Could not remove staff from database.", e);
            return false;
        }
    }

    public static bool AssignStaff(Ticket ticket, ulong staffID)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand update = new("UPDATE tickets SET assigned_staff_id = @assigned_staff_id WHERE id = @id", c);
            update.Parameters.AddWithValue("@assigned_staff_id", staffID);
            update.Parameters.AddWithValue("@id", ticket.id);
            update.Prepare();
            return update.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Warn("Could not add staff to ticket in database.", e);
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
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            MySqlCommand update = new("UPDATE staff SET active = @active WHERE user_id = @user_id", c);
            update.Parameters.AddWithValue("@user_id", staffID);
            update.Parameters.AddWithValue("@active", active);
            update.Prepare();
            return update.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Warn("Could not set staff member as active in database.", e);
            return false;
        }
    }

    public static List<StaffMember> GetActiveStaff(params ulong[] ignoredUserIDs)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM staff WHERE active = true AND user_id NOT IN (@user_ids)", c);
        selection.Parameters.AddWithValue("@user_ids", string.Join(",", ignoredUserIDs));
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return [];
        }

        List<StaffMember> staffMembers = [new(results)];
        while (results.Read())
        {
            staffMembers.Add(new StaffMember(results));
        }
        results.Close();

        return staffMembers;
    }

    public static List<StaffMember> GetAllStaff(params ulong[] ignoredUserIDs)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM staff WHERE user_id NOT IN (@user_ids)", c);
        selection.Parameters.AddWithValue("@user_ids", string.Join(",", ignoredUserIDs));
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return [];
        }

        List<StaffMember> staffMembers = [new(results)];
        while (results.Read())
        {
            staffMembers.Add(new StaffMember(results));
        }
        results.Close();

        return staffMembers;
    }

    public static bool IsStaff(ulong staffID)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM staff WHERE user_id=@user_id", c);
        selection.Parameters.AddWithValue("@user_id", staffID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return false;
        }
        results.Close();
        return true;
    }

    public static bool TryGetStaff(ulong staffID, out StaffMember staffMember)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new("SELECT * FROM staff WHERE user_id=@user_id", c);
        selection.Parameters.AddWithValue("@user_id", staffID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            staffMember = null;
            return false;
        }
        staffMember = new StaffMember(results);
        results.Close();
        return true;
    }
}