using System;
using MySql.Data.MySqlClient;

namespace SupportBot
{
	public static class Database
	{
		private static String connectionString = "";

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
			using (MySqlConnection c = GetConnection())
			{
				MySqlCommand createTickets = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS tickets(" +
					"id INT UNSIGNED NOT NULL UNIQUE PRIMARY KEY AUTO_INCREMENT," +
					"created_time TIMESTAMP NOT NULL," +
					"creator_id BIGINT UNSIGNED NOT NULL," +
					"channel_id BIGINT UNSIGNED NOT NULL UNIQUE," +
					"INDEX(created_time, channel_id))",
					c);
				MySqlCommand createBlacklisted = new MySqlCommand(
					"CREATE TABLE IF NOT EXISTS blacklisted_users(" +
					"user_id INT UNSIGNED NOT NULL UNIQUE PRIMARY KEY," +
					"time TIMESTAMP NOT NULL," +
					"moderator_id BIGINT UNSIGNED NOT NULL)",
					c);
				c.Open();
				createTickets.ExecuteNonQuery();
				createBlacklisted.ExecuteNonQuery();
			}
		}
	}
}
