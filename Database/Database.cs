using MySqlConnector;

namespace SupportBoi.Database;

public static class Connection
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
            "interview JSON NOT NULL," +
            "definitions JSON NOT NULL)",
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
}