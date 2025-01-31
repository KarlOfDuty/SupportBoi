using System.Collections.Generic;
using MySqlConnector;

namespace SupportBoi.Database;

public class Category(MySqlDataReader reader)
{
    public readonly string name = reader.GetString("name");
    public readonly ulong id = reader.GetUInt64("category_id");

    public static List<Category> GetAllCategories()
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM categories", c);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

        if (!results.Read())
        {
            return [];
        }

        List<Category> categories = [new(results)];
        while (results.Read())
        {
            categories.Add(new Category(results));
        }
        results.Close();

        return categories;
    }

    public static bool TryGetCategory(ulong categoryID, out Category message)
    {
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM categories WHERE category_id=@category_id", c);
        selection.Parameters.AddWithValue("@category_id", categoryID);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

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
        using MySqlConnection c = Connection.GetConnection();
        c.Open();
        using MySqlCommand selection = new MySqlCommand(@"SELECT * FROM categories WHERE name=@name", c);
        selection.Parameters.AddWithValue("@name", name);
        selection.Prepare();
        MySqlDataReader results = selection.ExecuteReader();

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
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"INSERT INTO categories (name,category_id) VALUES (@name, @category_id);", c);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@category_id", categoryID);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Error("Could not add category to database.", e);
            return false;
        }
    }

    public static bool RemoveCategory(ulong categoryID)
    {
        try
        {
            using MySqlConnection c = Connection.GetConnection();
            c.Open();
            using MySqlCommand cmd = new MySqlCommand(@"DELETE FROM categories WHERE category_id=@category_id", c);
            cmd.Parameters.AddWithValue("@category_id", categoryID);
            cmd.Prepare();
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException e)
        {
            Logger.Error("Could not remove category from database.", e);
            return false;
        }
    }
}